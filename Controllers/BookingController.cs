using CemaApp.Models;
using CemaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

namespace CemaApp.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly AppDbContext _context;

        public BookingController(IBookingService bookingService, AppDbContext context)
        {
            _bookingService = bookingService;
            _context = context;
        }

        // AJAX endpoint to get all seats for a screening
        [HttpGet]
        public async Task<IActionResult> GetSeats(int screeningId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var seats = await _bookingService.GetSeatsWithStatusAsync(screeningId, userId);
            return Json(seats);
        }

        // AJAX endpoint to lock/unlock a seat
        [HttpPost]
        public async Task<IActionResult> ToggleSeat(int screeningId, int seatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            var success = await _bookingService.LockSeatAsync(screeningId, seatId, userId);
            
            return Json(new { success });
        }

        [HttpGet]
        public async Task<IActionResult> SelectSeats(int screeningId)
        {
            var screening = await _context.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == screeningId);

            if (screening == null) return NotFound();

            // Check if there is an existing pending booking for this user and screening
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var pendingBooking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.ScreeningId == screeningId
                                       && b.UserId == userId
                                       && b.Status == BookingStatus.Pending);

            int remainingSeconds = 420; // Default 7 minutes for new sessions
            if (pendingBooking != null)
            {
                var elapsed = (DateTime.Now - pendingBooking.BookingDate).TotalSeconds;
                if (elapsed >= 420)
                {
                    await _bookingService.CleanExpiredPendingBookingsAsync();
                    TempData["ErrorMessage"] = "Your booking session has expired. Please select a movie to start again.";
                    return RedirectToAction("Index", "Movies");
                }
                remainingSeconds = 420 - (int)elapsed;
            }

            ViewBag.RemainingSeconds = remainingSeconds;

            return View(screening);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int screeningId, List<int> selectedSeatIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var success = await _bookingService.ConfirmBookingAsync(screeningId, selectedSeatIds, userId);

            if (success)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                return RedirectToAction("Index", "Bookings");
            }

            // Check if the pending booking session has expired
            var pendingBooking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.ScreeningId == screeningId
                                       && b.UserId == userId
                                       && b.Status == BookingStatus.Pending);

            if (pendingBooking == null || pendingBooking.BookingDate.AddMinutes(7) < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Your booking session has expired. Please select a movie to start again.";
                return RedirectToAction("Index", "Movies");
            }

            ModelState.AddModelError("", "Could not confirm booking. Your selection may have expired.");
            return RedirectToAction("SelectSeats", new { screeningId });
        }
    }
}
