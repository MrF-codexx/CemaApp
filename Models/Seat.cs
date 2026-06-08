using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    [Table("Seat")]

    public class Seat
    {
        public int Id { get; set; }

        public int HallId { get; set; }

        public string Row { get; set; } // A, B, C...

        public int Number { get; set; } // 1, 2, 3...

        // Navigation
        public Hall? Hall { get; set; }
        public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
    }
}
