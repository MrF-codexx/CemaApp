using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    [Table("BookingSeat")]

    public class BookingSeat
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        public int SeatId { get; set; }

        // Navigation
        public Booking? Booking { get; set; }
        public Seat? Seat { get; set; }
    }
}
