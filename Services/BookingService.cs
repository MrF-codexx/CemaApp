using CemaApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CemaApp.Services
{

    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "SeatLock";
        private const int LOCK_MINUTES = 7;

        public BookingService(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private string GetCacheKey(int screeningId, int seatId) => $"{CacheKeyPrefix}:{screeningId}:{seatId}";

        public async Task<bool> LockSeatAsync(int screeningId, int seatId, string userId)
        {
            // Clean up expired pending bookings first
            await CleanExpiredPendingBookingsAsync();

            // 1. Check if seat is permanently booked in DB
            var isBooked = await _context.BookingSeats
                .AnyAsync(bs => bs.Booking.ScreeningId == screeningId
                             && bs.SeatId == seatId
                             && bs.Booking.Status == BookingStatus.Confirmed);

            if (isBooked) return false;

            // 2. Check if seat is locked by another user via a Pending booking
            var existingPendingLock = await _context.BookingSeats
                .Include(bs => bs.Booking)
                .FirstOrDefaultAsync(bs => bs.Booking.ScreeningId == screeningId
                                         && bs.SeatId == seatId
                                         && bs.Booking.Status == BookingStatus.Pending
                                         && bs.Booking.UserId != userId);

            if (existingPendingLock != null)
            {
                // Check if the lock is still within the 7 min window
                if (existingPendingLock.Booking.BookingDate.AddMinutes(LOCK_MINUTES) > DateTime.Now)
                {
                    return false; // Locked by someone else
                }
            }

            var cacheKey = GetCacheKey(screeningId, seatId);

            // 3. Check if seat is already locked by the current user (Toggle/Deselect action)
            bool isLockedBySelfInCache = _cache.TryGetValue(cacheKey, out string? cachedUserId) && cachedUserId == userId;

            var pendingBooking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.ScreeningId == screeningId
                                       && b.UserId == userId
                                       && b.Status == BookingStatus.Pending);

            bool isLockedBySelfInDb = pendingBooking != null && pendingBooking.BookingSeats.Any(bs => bs.SeatId == seatId);

            if (isLockedBySelfInCache || isLockedBySelfInDb)
            {
                // Remove lock from cache
                _cache.Remove(cacheKey);

                // Remove seat from pending booking in DB
                if (pendingBooking != null)
                {
                    var seatToRemove = pendingBooking.BookingSeats.FirstOrDefault(bs => bs.SeatId == seatId);
                    if (seatToRemove != null)
                    {
                        _context.BookingSeats.Remove(seatToRemove);
                        
                        var screening = await _context.Screenings.FindAsync(screeningId);
                        if (screening != null)
                        {
                            int remainingCount = pendingBooking.BookingSeats.Count - 1;
                            if (remainingCount > 0)
                            {
                                pendingBooking.TotalPrice = remainingCount * screening.Price;
                            }
                            else
                            {
                                // No seats left, clean up the pending booking header
                                _context.Bookings.Remove(pendingBooking);
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                return true;
            }

            // 4. Check if seat is locked in MemoryCache by someone else
            if (_cache.TryGetValue(cacheKey, out string? existingUserId) && existingUserId != userId)
            {
                return false; // Locked by someone else
            }

            // 5. Set the lock with 7-minute expiration in cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(LOCK_MINUTES));

            _cache.Set(cacheKey, userId, cacheOptions);

            // 6. Create a new Pending booking if one doesn't exist
            if (pendingBooking == null)
            {
                var screening = await _context.Screenings.FindAsync(screeningId);
                if (screening == null) return false;

                pendingBooking = new Booking
                {
                    UserId = userId,
                    ScreeningId = screeningId,
                    BookingDate = DateTime.Now,
                    TotalPrice = screening.Price,
                    Status = BookingStatus.Pending
                };
                _context.Bookings.Add(pendingBooking);
                await _context.SaveChangesAsync();
            }

            // 7. Add seat to the pending booking
            var alreadyAdded = pendingBooking.BookingSeats.Any(bs => bs.SeatId == seatId);
            if (!alreadyAdded)
            {
                _context.BookingSeats.Add(new BookingSeat
                {
                    BookingId = pendingBooking.Id,
                    SeatId = seatId
                });

                var screening2 = await _context.Screenings.FindAsync(screeningId);
                if (screening2 != null)
                {
                    pendingBooking.TotalPrice = (pendingBooking.BookingSeats.Count + 1) * screening2.Price;
                }

                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> ConfirmBookingAsync(int screeningId, List<int> seatIds, string userId)
        {
            // 1. Verify locks still exist for this user in MemoryCache
            foreach (var seatId in seatIds)
            {
                var cacheKey = GetCacheKey(screeningId, seatId);
                if (!_cache.TryGetValue(cacheKey, out string? lockedUser) || lockedUser != userId)
                {
                    return false; // Lock expired or doesn't belong to user
                }
            }

            // 2. Look for the existing Pending booking
            var pendingBooking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.ScreeningId == screeningId
                                       && b.UserId == userId
                                       && b.Status == BookingStatus.Pending);

            if (pendingBooking != null)
            {
                // Update existing pending booking to Confirmed
                var screening = await _context.Screenings.FindAsync(screeningId);
                if (screening == null) return false;

                pendingBooking.Status = BookingStatus.Confirmed;
                pendingBooking.BookingDate = DateTime.Now;
                pendingBooking.TotalPrice = seatIds.Count * screening.Price;

                // Remove old seats and add the confirmed ones
                _context.BookingSeats.RemoveRange(pendingBooking.BookingSeats);
                foreach (var seatId in seatIds)
                {
                    _context.BookingSeats.Add(new BookingSeat
                    {
                        BookingId = pendingBooking.Id,
                        SeatId = seatId
                    });
                    _cache.Remove(GetCacheKey(screeningId, seatId));
                }

                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                // Fallback: create a new Confirmed booking (original behavior)
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
                    _cache.Remove(GetCacheKey(screeningId, seatId));
                }

                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<List<SeatDto>> GetSeatsWithStatusAsync(int screeningId, string userId)
        {
            var screening = await _context.Screenings
                .Include(s => s.Hall)
                .ThenInclude(h => h.Seats)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == screeningId);

            if (screening == null) return new List<SeatDto>();

            // Get all booked seats for this screening in one DB hit
            var bookedSeatIds = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.ScreeningId == screeningId && bs.Booking.Status == BookingStatus.Confirmed)
                .Select(bs => bs.SeatId)
                .ToListAsync();

            // Get all pending-locked seats from other users
            var pendingLockedSeatIds = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.ScreeningId == screeningId
                          && bs.Booking.Status == BookingStatus.Pending
                          && bs.Booking.UserId != userId
                          && bs.Booking.BookingDate.AddMinutes(LOCK_MINUTES) > DateTime.Now)
                .Select(bs => bs.SeatId)
                .ToListAsync();

            // Get all pending-locked seats from the current user (fallback if cache is cleared)
            var currentUserPendingSeatIds = await _context.BookingSeats
                .AsNoTracking()
                .Where(bs => bs.Booking.ScreeningId == screeningId
                          && bs.Booking.Status == BookingStatus.Pending
                          && bs.Booking.UserId == userId
                          && bs.Booking.BookingDate.AddMinutes(LOCK_MINUTES) > DateTime.Now)
                .Select(bs => bs.SeatId)
                .ToListAsync();

            var seats = new List<SeatDto>();

            foreach (var seat in screening.Hall.Seats)
            {
                var state = SeatState.Available;

                if (bookedSeatIds.Contains(seat.Id))
                {
                    state = SeatState.Booked;
                }
                else if (pendingLockedSeatIds.Contains(seat.Id))
                {
                    state = SeatState.Locked;
                }
                else if (currentUserPendingSeatIds.Contains(seat.Id))
                {
                    state = SeatState.Selected;
                }
                else
                {
                    var cacheKey = GetCacheKey(screeningId, seat.Id);
                    if (_cache.TryGetValue(cacheKey, out string? lockedUserId))
                    {
                        state = (lockedUserId == userId) ? SeatState.Selected : SeatState.Locked;
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
