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
        public async Task<IActionResult> GetSeats(int screeningId)
        {
            var sessionId = HttpContext.Session.Id;
            var seats = await _bookingService.GetSeatsWithStatusAsync(screeningId, sessionId);
            return Json(seats);
        }

        // AJAX endpoint to lock/unlock a seat
        [HttpPost]
        public IActionResult ToggleSeat(int screeningId, int seatId)
        {
            var sessionId = HttpContext.Session.Id;
            var sessionKey = $"seats_{screeningId}";
            var selectedSeatsStr = HttpContext.Session.GetString(sessionKey);
            var selectedSeats = string.IsNullOrEmpty(selectedSeatsStr) 
                ? new List<int>() 
                : selectedSeatsStr.Split(',').Select(int.Parse).ToList();

            if (selectedSeats.Contains(seatId))
            {
                _seatCache.Release(screeningId, seatId, sessionId);
                selectedSeats.Remove(seatId);
                if (selectedSeats.Any())
                    HttpContext.Session.SetString(sessionKey, string.Join(",", selectedSeats));
                else
                    HttpContext.Session.Remove(sessionKey);
                
                return Ok(new { message = "released", success = true });
            }

            if (!_seatCache.TryHold(screeningId, seatId, sessionId))
            {
                return StatusCode(409, new { message = "taken", success = false });
            }

            selectedSeats.Add(seatId);
            HttpContext.Session.SetString(sessionKey, string.Join(",", selectedSeats));
            return Ok(new { message = "held", success = true });
        }

        // Page to show seat selection
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
                remainingSeconds = Math.Max(0, 420 - (int)elapsed);
            }

            ViewBag.RemainingSeconds = remainingSeconds;

            return View(screening);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int screeningId, List<int> selectedSeatIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var sessionId = HttpContext.Session.Id;
            try
            {
                var success = await _bookingService.ConfirmBookingAsync(screeningId, selectedSeatIds, userId, sessionId);

                if (success)
                {
                    var sessionId = HttpContext.Session.Id;
                    foreach (var seatId in selectedSeatIds)
                    {
                        _seatCache.Release(screeningId, seatId, sessionId);
                    }
                    HttpContext.Session.Remove($"seats_{screeningId}");

                    if (User.IsInRole("Admin"))
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                    return RedirectToAction("Index", "Bookings");
                }

                ModelState.AddModelError("", "Could not confirm booking. Your selection may have expired.");
                return RedirectToAction("SelectSeats", new { screeningId });
            }
            catch (SeatAlreadyBookedException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Booking failed. Please try again." });
            }
        }
    }
}
