using CemaApp.Models;
using CemaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace CemaApp.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IBookingService _bookingService;
        private readonly IMemoryCache _cache;

        public BookingsController(AppDbContext context, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager, IBookingService bookingService, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _bookingService = bookingService;
            _cache = cache;
        }

        // GET: Bookings (User's History & Profile)
        public async Task<IActionResult> Index()
        {
            // Clean expired pending bookings first
            await _bookingService.CleanExpiredPendingBookingsAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null) return NotFound();

            // Pending bookings that are still within the 7-minute window
            var pendingBookings = await _context.Bookings
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Hall)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .Where(b => b.UserId == userId 
                         && b.Status == BookingStatus.Pending
                         && b.BookingDate.AddMinutes(7) > DateTime.Now)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            // History: Confirmed and Cancelled bookings
            var bookings = await _context.Bookings
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Hall)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .Where(b => b.UserId == userId && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Cancelled))
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var viewModel = new CemaApp.ViewModels.UserBookingsViewModel
            {
                User = user,
                PendingBookings = pendingBookings,
                Bookings = bookings
            };

            return View(viewModel);
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Hall)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (booking == null) return NotFound();

            return View(booking);
        }
        // POST: Bookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null) return NotFound();

            // Only allow cancellation if status is not already Cancelled
            if (booking.Status != BookingStatus.Cancelled)
            {
                // Release memory cache locks for all seats associated with this booking
                foreach (var bs in booking.BookingSeats)
                {
                    var cacheKey = $"SeatLock:{booking.ScreeningId}:{bs.SeatId}";
                    _cache.Remove(cacheKey);
                }

                booking.Status = BookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Booking cancelled successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Bookings/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.Cancelled)
            {
                booking.Status = BookingStatus.RemovedByUser;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Booking removed from history.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
