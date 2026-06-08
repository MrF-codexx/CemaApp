using CemaApp.Models;

namespace CemaApp.ViewModels
{
    public class UserBookingsViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Booking> PendingBookings { get; set; } = new List<Booking>();
        public List<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
