using CemaApp.Models;

namespace CemaApp.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalUsers { get; set; }
        public Movie MostPopularMovie { get; set; }
        public int PopularMovieBookingCount { get; set; }
        public double AverageOccupancyRate { get; set; }
        public List<RecentBookingViewModel> RecentBookings { get; set; } = new List<RecentBookingViewModel>();
    }

    public class RecentBookingViewModel
    {
        public int BookingId { get; set; }
        public string UserEmail { get; set; }
        public string MovieTitle { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal Amount { get; set; }
        public BookingStatus Status { get; set; }
    }
}
