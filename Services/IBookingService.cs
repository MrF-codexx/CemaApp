using CemaApp.Models;

namespace CemaApp.Services
{
    public interface IBookingService
    {
        Task<bool> LockSeatAsync(int screeningId, int seatId, string userId);
        Task<bool> ConfirmBookingAsync(int screeningId, List<int> seatIds, string userId);
        Task<List<SeatDto>> GetSeatsWithStatusAsync(int screeningId, string userId);
        Task CleanExpiredPendingBookingsAsync();
    }

    public class SeatDto
    {
        public int Id { get; set; }
        public string Row { get; set; }
        public int Number { get; set; }
        public string State { get; set; } // "Available", "Selected", "Locked", "Booked"
    }

    public enum SeatState
    {
        Available,
        Selected,
        Locked,
        Booked
    }
}
