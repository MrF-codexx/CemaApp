using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    [Table("User")]
    public class ApplicationUser : IdentityUser
    {
        // Identity already provides:
        // - Id (string)
        // - Email
        // - PasswordHash (secure)
        // - PhoneNumber
        // - EmailConfirmed
        [Required]
        public string FullName { get; set; }
        [Required]

        public DateTime DateOfBirth { get; set; }

        // Navigation properties
        public ICollection<Booking> Bookings { get; set; }


    }
}
