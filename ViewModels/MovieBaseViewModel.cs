using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CemaApp.ViewModels
{
    // 1. The Base Class: Holds only the properties shared between Create and Edit
    public abstract class MovieBaseViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Genre { get; set; }

        [Required]
        [Range(1, 500, ErrorMessage = "Duration must be between 1 and 500 minutes")]
        public int DurationMinutes { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "YouTube Trailer URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? TrailerUrl { get; set; }
    }

    // 2. The Create Class: Inherits the base properties and adds what is strictly needed for Creation
    public class MovieCreateViewModel : MovieBaseViewModel
    {
        [Required(ErrorMessage = "Please upload a poster image.")]
        public IFormFile PosterImage { get; set; }
    }

    // 3. The Edit Class: Inherits the base properties and adds what is strictly needed for Editing
    public class MovieEditViewModel : MovieBaseViewModel
    {
        public int Id { get; set; }

        public bool IsActive { get; set; }

        // Tracks the image currently saved in the database
        public string? ExistingPosterUrl { get; set; }

        // Captures a new image if the Admin decides to upload one (Optional)
        public IFormFile? NewPosterImage { get; set; }
    }
}