using CemaApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CemaApp.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly ISeatReservationCache _seatCache;
        private readonly ILogger<BookingService> _logger;
        private const int LOCK_MINUTES = 7;

        public BookingService(AppDbContext context, ISeatReservationCache seatCache, ILogger<BookingService> logger)
        {
            _context = context;
            _seatCache = seatCache;
            _logger = logger;
        }

        public async Task<bool> LockSeatAsync(int screeningId, int seatId, string userId)
        {
            // Now handled in controller, but implemented to satisfy interface and DI cache update rule
            return _seatCache.TryHold(screeningId, seatId, userId);
        }

        public async Task<bool> ConfirmBookingAsync(int screeningId, List<int> seatIds, string userId, string sessionId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify that every single seat is actively held in the memory cache by THIS EXACT session!
                // This strictly guarantees that no one can bypass the frontend and double book!
                foreach (var seatId in seatIds)
                {
                    if (!_seatCache.IsHeldBy(screeningId, seatId, sessionId))
                    {
                        throw new SeatAlreadyBookedException("One of your selected seats is no longer held by your session. It may have expired.");
                    }
                }

                var screening = await _context.Screenings.FindAsync(screeningId);
                if (screening == null) return false;

                var booking = new Booking
                {
                    UserId = userId,
                    ScreeningId = screeningId,
                    BookingDate = DateTime.Now,
                    TotalPrice = seatIds.Count * screening.Price,
                    Status = BookingStatus.Confirmed
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                foreach (var seatId in seatIds)
                {
                    _context.BookingSeats.Add(new BookingSeat
                    {
                        BookingId = booking.Id,
                        SeatId = seatId
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(
                    ex,
                    "Unique constraint violation during seat confirm. ScreeningId={ScreeningId}",
                    screeningId);
                throw new SeatAlreadyBookedException(
                    "One or more seats were just booked by someone else. Please reselect.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    ex,
                    "Unexpected booking failure. ScreeningId={ScreeningId}",
                    screeningId);
                throw; // let the controller return 500
            }
        }

        public async Task<List<SeatDto>> GetSeatsWithStatusAsync(int screeningId, string userId)
        {
            // userId here acts as sessionId due to the ToggleSeat logic changes
            var screening = await _context.Screenings
                .Include(s => s.Hall)
                .ThenInclude(h => h.Seats)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == screeningId);

            if (screening == null) return new List<SeatDto>();

            var bookedSeatIds = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.ScreeningId == screeningId && bs.Booking.Status == BookingStatus.Confirmed)
                .Select(bs => bs.SeatId)
                .ToListAsync();

            var seats = new List<SeatDto>();
            var allHolds = _seatCache.GetAllHolds();

            foreach (var seat in screening.Hall.Seats)
            {
                var state = SeatState.Available;

                if (bookedSeatIds.Contains(seat.Id))
                {
                    state = SeatState.Booked;
                }
                else
                {
                    var key = $"{screeningId}_{seat.Id}";
                    if (allHolds.TryGetValue(key, out var hold) && hold.ExpiresAt > DateTime.UtcNow)
                    {
                        if (hold.SessionId == userId) // userId is actually sessionId
                        {
                            state = SeatState.Selected;
                        }
                        else
                        {
                            state = SeatState.Locked;
                        }
                    }
                }

                seats.Add(new SeatDto
                {
                    Id = seat.Id,
                    Row = seat.Row,
                    Number = seat.Number,
                    State = state.ToString()
                });
            }

            return seats;
        }

        public async Task CleanExpiredPendingBookingsAsync()
        {
            var cutoff = DateTime.Now.AddMinutes(-LOCK_MINUTES);
            var expired = await _context.Bookings
                .Include(b => b.BookingSeats)
                .Where(b => b.Status == BookingStatus.Pending && b.BookingDate < cutoff)
                .ToListAsync();

            if (expired.Any())
            {
                foreach (var booking in expired)
                {
                    _context.BookingSeats.RemoveRange(booking.BookingSeats);
                }
                _context.Bookings.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
        }
    }
}
