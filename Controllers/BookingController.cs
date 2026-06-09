using CemaApp.Models;
using CemaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CemaApp.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly AppDbContext _context;
        private readonly ISeatReservationCache _seatCache;

        public BookingController(IBookingService bookingService, AppDbContext context, ISeatReservationCache seatCache)
        {
            _bookingService = bookingService;
            _context = context;
            _seatCache = seatCache;
        }

        // AJAX endpoint to get all seats for a screening
        [HttpGet]
        public async Task<IActionResult> GetSeats(int screeningId, string clientSessionId)
        {
            if (string.IsNullOrEmpty(clientSessionId))
            {
                return BadRequest("clientSessionId is required");
            }
            var seats = await _bookingService.GetSeatsWithStatusAsync(screeningId, clientSessionId);

            // Calculate remaining seconds for this clientSessionId
            var holds = _seatCache.GetAllHolds()
                .Where(kvp => kvp.Key.StartsWith($"{screeningId}_") && kvp.Value.SessionId == clientSessionId)
                .ToList();

            int remainingSeconds = 420; // Default 7 minutes
            if (holds.Any())
            {
                var oldestHold = holds.Min(h => h.Value.ExpiresAt);
                var elapsed = (oldestHold - DateTime.UtcNow).TotalSeconds;
                remainingSeconds = Math.Max(0, (int)elapsed);
            }

            return Json(new { seats, remainingSeconds });
        }

        // AJAX endpoint to lock/unlock a seat
        [HttpPost]
        public IActionResult ToggleSeat(int screeningId, int seatId, string clientSessionId)
        {
            if (string.IsNullOrEmpty(clientSessionId))
            {
                return BadRequest("clientSessionId is required");
            }

            if (_seatCache.IsHeldBy(screeningId, seatId, clientSessionId))
            {
                _seatCache.Release(screeningId, seatId, clientSessionId);
                return Ok(new { message = "released", success = true });
            }

            if (!_seatCache.TryHold(screeningId, seatId, clientSessionId))
            {
                return StatusCode(409, new { message = "taken", success = false });
            }

            return Ok(new { message = "held", success = true });
        }

        [HttpGet]
        public async Task<IActionResult> SelectSeats(int screeningId)
        {
            var screening = await _context.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == screeningId);

            if (screening == null) return NotFound();

            // Default timer starting point. Updated dynamically via client AJAX.
            ViewBag.RemainingSeconds = 420;

            return View(screening);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int screeningId, List<int> selectedSeatIds, string clientSessionId)
        {
            if (string.IsNullOrEmpty(clientSessionId))
            {
                TempData["ErrorMessage"] = "Session identifier is missing. Please refresh and try again.";
                return RedirectToAction("SelectSeats", new { screeningId });
            }

            if (selectedSeatIds == null || !selectedSeatIds.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one seat.";
                return RedirectToAction("SelectSeats", new { screeningId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            try
            {
                var success = await _bookingService.ConfirmBookingAsync(screeningId, selectedSeatIds, userId, clientSessionId);

                if (success)
                {
                    if (User.IsInRole("Admin"))
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                    return RedirectToAction("Index", "Bookings");
                }

                TempData["ErrorMessage"] = "Could not confirm booking. Your selection may have expired.";
                return RedirectToAction("SelectSeats", new { screeningId });
            }
            catch (SeatAlreadyBookedException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("SelectSeats", new { screeningId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Booking failed. Please try again.";
                return RedirectToAction("SelectSeats", new { screeningId });
            }
        }
    }
}
