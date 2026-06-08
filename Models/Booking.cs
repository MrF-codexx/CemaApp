using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        RemovedByUser
    }
    [Table("Booking")]
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ScreeningId { get; set; }

        public DateTime BookingDate { get; set; }

        public decimal TotalPrice { get; set; }

        
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        // Navigation
        public ApplicationUser? User { get; set; }
        public Screening? Screening { get; set; }
        public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    }
}
