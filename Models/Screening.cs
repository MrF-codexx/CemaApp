using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    [Table("Screening")]
    public class Screening
    {
        public int Id { get; set; }

        public int MovieId { get; set; }

        public int HallId { get; set; }

        public DateTime StartTime { get; set; }

        public decimal Price { get; set; }

        // Navigation
        public Movie? Movie { get; set; }
        public Hall? Hall { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
