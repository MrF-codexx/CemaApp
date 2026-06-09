using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    [Table("Seat")]

    public class Seat
    {
        public int Id { get; set; }

        public int HallId { get; set; }

        public string Row { get; set; } = string.Empty; 
        public int Number { get; set; } 
        public Hall? Hall { get; set; }
        public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
    }
}
