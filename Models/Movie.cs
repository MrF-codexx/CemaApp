using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CemaApp.Models
{
    [Table("Movie")]
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string Genre { get; set; }

        public int DurationMinutes { get; set; }

        public DateTime ReleaseDate { get; set; }

        public string? PosterUrl { get; set; }
        
        public string? TrailerUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
    }
}
