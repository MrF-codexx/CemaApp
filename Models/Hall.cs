using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    [Table("Hall")]

    public class Hall
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int TotalRows { get; set; }

        public int SeatsPerRow { get; set; }

        // Navigation
        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
