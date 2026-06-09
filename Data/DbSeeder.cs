using CemaApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CemaApp.Data
{
    public class TmdbMovieResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("overview")]
        public string Overview { get; set; }
        
        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; }
        
        [JsonPropertyName("poster_path")]
        public string PosterPath { get; set; }
        
        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; }
    }

    public class TmdbApiResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbMovieResult> Results { get; set; }
    }

    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed Roles
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            string adminEmail = "admin@cema.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator Hola",
                    DateOfBirth = new DateTime(2004, 1, 1),
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        public static async Task SeedSampleDataAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            // 1. Check if we've already seeded any movies
            if (context.Movies.Any())
            {
                return; // Already seeded, skip everything!
            }

            // 3. SEED USERS
            var users = new List<ApplicationUser>();
            string[] testEmails = { "john@cema.com", "sarah@cema.com", "mike@cema.com", "emma@cema.com" };
            foreach (var email in testEmails)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = "Test User " + email.Split('@')[0],
                    DateOfBirth = new DateTime(1990, 1, 1),
                    EmailConfirmed = true
                };
                
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    await userManager.CreateAsync(user, "User@123");
                    await userManager.AddToRoleAsync(user, "User");
                }
                var dbUser = await userManager.FindByEmailAsync(email);
                if (dbUser != null) users.Add(dbUser);
            }

            // 4. SEED HALLS AND SEATS
            var halls = new List<Hall>
            {
                new Hall { Name = "IMAX 1", TotalRows = 10, SeatsPerRow = 15 },
                new Hall { Name = "Standard 2", TotalRows = 8, SeatsPerRow = 10 },
                new Hall { Name = "VIP Lounge", TotalRows = 5, SeatsPerRow = 8 }
            };
            context.Halls.AddRange(halls);
            await context.SaveChangesAsync();

            foreach (var hall in halls)
            {
                char[] rowLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
                for (int r = 0; r < hall.TotalRows; r++)
                {
                    for (int s = 1; s <= hall.SeatsPerRow; s++)
                    {
                        context.Seats.Add(new Seat
                        {
                            HallId = hall.Id,
                            Row = rowLetters[r].ToString(),
                            Number = s
                        });
                    }
                }
            }
            await context.SaveChangesAsync();

            // 5. SEED MOVIES from data.json
            var movies = new List<Movie>();
            var dataJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "data.json");
            
            if (File.Exists(dataJsonPath))
            {
                var jsonContent = await File.ReadAllTextAsync(dataJsonPath);
                var tmdbData = JsonSerializer.Deserialize<TmdbApiResponse>(jsonContent);
                
                if (tmdbData?.Results != null)
                {
                    foreach (var item in tmdbData.Results)
                    {
                        DateTime.TryParse(item.ReleaseDate, out DateTime parsedDate);
                        
                        // Map TMDB genre IDs to strings (simplified mapping)
                        string genre = "Action";
                        if (item.GenreIds != null && item.GenreIds.Any())
                        {
                            if (item.GenreIds.Contains(16)) genre = "Animation";
                            else if (item.GenreIds.Contains(27)) genre = "Horror";
                            else if (item.GenreIds.Contains(35)) genre = "Comedy";
                            else if (item.GenreIds.Contains(878)) genre = "Sci-Fi";
                            else if (item.GenreIds.Contains(10749)) genre = "Romance";
                        }

                        movies.Add(new Movie
                        {
                            Title = item.Title,
                            Description = item.Overview,
                            ReleaseDate = parsedDate != default ? parsedDate : DateTime.Now,
                            PosterUrl = !string.IsNullOrEmpty(item.PosterPath) ? $"https://image.tmdb.org/t/p/w500{item.PosterPath}" : null,
                            Genre = genre,
                            DurationMinutes = new Random().Next(90, 150), // Random duration since TMDB result doesn't have it here
                            TrailerUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Dummy trailer
                            IsActive = true
                        });
                    }
                }
            }

            if (movies.Any())
            {
                context.Movies.AddRange(movies);
                await context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine("Warning: No movies were loaded from data.json.");
                return;
            }

            // 6. SEED SCREENINGS
            var screenings = new List<Screening>();
            Random rand = new Random();
            foreach (var movie in movies)
            {
                for (int i = 0; i < 3; i++) // 3 screenings per movie
                {
                    var hall = halls[rand.Next(halls.Count)];

                    // Assign pricing dynamically based on the hall type
                    decimal price = 12.00m; // Default Standard price
                    if (hall.Name.Contains("IMAX"))
                    {
                        price = 18.00m;
                    }
                    else if (hall.Name.Contains("VIP"))
                    {
                        price = 28.00m;
                    }

                    screenings.Add(new Screening
                    {
                        MovieId = movie.Id,
                        HallId = hall.Id,
                        StartTime = DateTime.Now.AddDays(rand.Next(1, 7)).AddHours(rand.Next(10, 22)),
                        Price = price
                    });
                }
            }
            context.Screenings.AddRange(screenings);
            await context.SaveChangesAsync();

            // 7. SEED BOOKINGS
            var allSeats = await context.Seats.ToListAsync();
            foreach (var user in users)
            {
                int numberOfBookings = rand.Next(1, 4);
                for (int i = 0; i < numberOfBookings; i++)
                {
                    var screening = screenings[rand.Next(screenings.Count)];
                    var booking = new Booking
                    {
                        UserId = user.Id,
                        ScreeningId = screening.Id,
                        BookingDate = DateTime.Now.AddDays(-rand.Next(1, 5)),
                        TotalPrice = screening.Price * 2, // Assume 2 seats
                        Status = BookingStatus.Confirmed // Set to Confirmed so they don't get auto-deleted immediately by the cache purge
                    };
                    context.Bookings.Add(booking);
                    await context.SaveChangesAsync();

                    // Add 2 UNIQUE random seats to this booking
                    var hallSeats = allSeats.Where(s => s.HallId == screening.HallId).ToList();
                    if (hallSeats.Count >= 2)
                    {
                        // Use OrderBy Guid to shuffle and pick 2 distinct seats
                        var selectedSeats = hallSeats.OrderBy(x => Guid.NewGuid()).Take(2).ToList();
                        foreach (var seat in selectedSeats)
                        {
                            context.BookingSeats.Add(new BookingSeat
                            {
                                BookingId = booking.Id,
                                SeatId = seat.Id
                            });
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}