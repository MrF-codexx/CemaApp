using CemaApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Data
{
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

            // 2. We no longer wipe existing data here to prevent accidental data loss.
            // Data seeding will only occur if the database is completely empty of movies.

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
                await userManager.CreateAsync(user, "User@123");
                await userManager.AddToRoleAsync(user, "User");
                users.Add(user);
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

            // 5. SEED MOVIES
            var movies = new List<Movie>
            {
                new Movie
                {
                    Title = "The Super Mario Bros. Movie",
                    Genre = "Animation",
                    DurationMinutes = 92,
                    ReleaseDate = new DateTime(2023, 4, 5),
                    Description = "While working underground to fix a water main, Brooklyn plumbers—and brothers—Mario and Luigi are transported down a mysterious pipe and wander into a magical new world. But when the brothers are separated, Mario embarks on an epic quest to find Luigi.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/qNBAXBIQlnOThrVvA6mA2B5ggV6.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/RjNcTBgV4XY",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Shazam! Fury of the Gods",
                    Genre = "Action",
                    DurationMinutes = 130,
                    ReleaseDate = new DateTime(2023, 3, 15),
                    Description = "Billy Batson and his foster siblings, who transform into superheroes by saying \"Shazam!\", are forced to get back into action and fight the Daughters of Atlas, who they must stop from using a weapon that could destroy the world.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/2VK4d3mqqTc7LVZLnLPeRiPaJ71.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/Zi88i4CpHe4",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Evil Dead Rise",
                    Genre = "Horror",
                    DurationMinutes = 96,
                    ReleaseDate = new DateTime(2023, 4, 12),
                    Description = "Two sisters find an ancient vinyl that gives birth to bloodthirsty demons that run amok in a Los Angeles apartment building and thrusts them into a primal battle for survival as they face the most nightmarish version of family imaginable.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/mIBCtPvKZQlxubxKMeViO2UrP3q.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/BqQNO7Bzf08",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Puss in Boots: The Last Wish",
                    Genre = "Animation",
                    DurationMinutes = 102,
                    ReleaseDate = new DateTime(2022, 12, 7),
                    Description = "Puss in Boots discovers that his passion for adventure has taken its toll: He has burned through eight of his nine lives, leaving him with only one life left. Puss sets out on an epic journey to find the mythical Last Wish and restore his nine lives.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/kuf6dutpsT0vSVehic3EZIqkOBt.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/RqrXhwS33yc",
                    IsActive = true
                },
                new Movie
                {
                    Title = "John Wick: Chapter 4",
                    Genre = "Action",
                    DurationMinutes = 169,
                    ReleaseDate = new DateTime(2023, 3, 22),
                    Description = "With the price on his head ever increasing, John Wick uncovers a path to defeating The High Table. But before he can earn his freedom, Wick must face off against a new enemy with powerful alliances across the globe and forces that turn old friends into foes.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/vZloFAK7NmvMGKE7VkF5UHaz0I.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/qEVUardpmT4",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Cocaine Bear",
                    Genre = "Thriller",
                    DurationMinutes = 95,
                    ReleaseDate = new DateTime(2023, 2, 22),
                    Description = "Inspired by a true story, an oddball group of cops, criminals, tourists and teens converge in a Georgia forest where a 500-pound black bear goes on a murderous rampage after unintentionally ingesting cocaine.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/gOnmaxHo0412UVr1QM5Nekv1xPi.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/DuWEEKeJLMI",
                    IsActive = true
                },
                new Movie
                {
                    Title = "The Communion Girl",
                    Genre = "Horror",
                    DurationMinutes = 98,
                    ReleaseDate = new DateTime(2023, 2, 10),
                    Description = "Spain, late 1980s. Newcomer Sara tries to fit in with the other teens in this tight-knit small town in the province of Tarragona. If only she were more like her extroverted best friend, Rebe. They go out one night at a nightclub, on the way home, they come upon a little girl holding a doll, dressed for her first communion. And that's when the nightmare begins.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/sP6AO11a7jWgsmT9T8j9EGIWAaZ.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/s9xezlAwtD0",
                    IsActive = true
                },
                new Movie
                {
                    Title = "65",
                    Genre = "Sci-Fi",
                    DurationMinutes = 93,
                    ReleaseDate = new DateTime(2023, 3, 2),
                    Description = "65 million years ago, the only 2 survivors of a spaceship from Somaris that crash-landed on Earth must fend off dinosaurs and reach the escape vessel in time before an imminent asteroid strike threatens to destroy the planet.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/rzRb63TldOKdKydCvWJM8B6EkPM.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/bHXejJq5vh0",
                    IsActive = true
                },
                new Movie
                {
                    Title = "The Pope's Exorcist",
                    Genre = "Horror",
                    DurationMinutes = 103,
                    ReleaseDate = new DateTime(2023, 4, 5),
                    Description = "Father Gabriele Amorth, Chief Exorcist of the Vatican, investigates a young boy's terrifying possession and ends up uncovering a centuries-old conspiracy the Vatican has desperately tried to keep hidden.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/9JBEPLTPSm0d1mbEcLxULjJq9Eh.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/YJXqvnT_rsk",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Supercell",
                    Genre = "Action",
                    DurationMinutes = 100,
                    ReleaseDate = new DateTime(2023, 3, 17),
                    Description = "Good-hearted teenager William always lived in hope of following in his late father’s footsteps and becoming a storm chaser. His father’s legacy has now been turned into a storm-chasing tourist business, managed by the greedy and reckless Zane Rogers, who is now using William as the main attraction to lead a group of unsuspecting adventurers deep into the eye of the most dangerous supercell ever seen.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/gbGHezV6yrhua0KfAgwrknSOiIY.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/pXQvMsk06i0",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Ghosted",
                    Genre = "Comedy",
                    DurationMinutes = 116,
                    ReleaseDate = new DateTime(2023, 4, 21),
                    Description = "Salt-of-the-earth Cole falls head over heels for enigmatic Sadie — but then makes the shocking discovery that she’s a secret agent. Before they can decide on a second date, Cole and Sadie are swept away on an international adventure to save the world.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/liLN69YgoovHVgmlHJ876PKi5Yi.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/I6-0SUtF4cE",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Scream VI",
                    Genre = "Horror",
                    DurationMinutes = 122,
                    ReleaseDate = new DateTime(2023, 3, 8),
                    Description = "Following the latest Ghostface killings, the four survivors leave Woodsboro behind and start a fresh chapter.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/wDWwtvkRRlgTiUr6TyLSMX8FCuZ.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/h74AXqw4Opc",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Mummies",
                    Genre = "Animation",
                    DurationMinutes = 88,
                    ReleaseDate = new DateTime(2023, 1, 5),
                    Description = "Through a series of unfortunate events, three mummies end up in present-day London and embark on a wacky and hilarious journey in search of an old ring belonging to the Royal Family, stolen by ambitious archaeologist Lord Carnaby.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/qVdrYN8qu7xUtsdEFeGiIVIaYd.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/xsk9YgLw8y0",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Winnie the Pooh: Blood and Honey",
                    Genre = "Horror",
                    DurationMinutes = 84,
                    ReleaseDate = new DateTime(2023, 1, 27),
                    Description = "Christopher Robin is headed off to college and he has abandoned his old friends, Pooh and Piglet, which then leads to the duo embracing their inner monsters.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/ewF3IlGscc7FjgGEPcQvZsAsgAW.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/W3E74j_ZF34",
                    IsActive = true
                },
                new Movie
                {
                    Title = "M3GAN",
                    Genre = "Sci-Fi",
                    DurationMinutes = 102,
                    ReleaseDate = new DateTime(2022, 12, 28),
                    Description = "A brilliant toy company roboticist uses artificial intelligence to develop M3GAN, a life-like doll programmed to emotionally bond with her newly orphaned niece. But when the doll's programming works too well, she becomes overprotective of her new friend with terrifying results.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/d9nBoowhjiiYc4FBNtQkPY7c11H.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/BRb4U99OU80",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Knock at the Cabin",
                    Genre = "Horror",
                    DurationMinutes = 100,
                    ReleaseDate = new DateTime(2023, 2, 1),
                    Description = "While vacationing at a remote cabin, a young girl and her two fathers are taken hostage by four armed strangers who demand that the family make an unthinkable choice to avert the apocalypse. With limited access to the outside world, the family must decide what they believe before all is lost.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/dm06L9pxDOL9jNSK4Cb6y139rrG.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/0W8aC_vIPt0",
                    IsActive = true
                },
                new Movie
                {
                    Title = "The Amazing Maurice",
                    Genre = "Animation",
                    DurationMinutes = 93,
                    ReleaseDate = new DateTime(2022, 12, 16),
                    Description = "Maurice is a streetwise ginger cat who comes up with a money-making scam by befriending a group of self-taught talking rats. When Maurice and the rodents meet a bookworm called Malicia, their little con soon goes down the drain.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/ydhZeUjbzVEFclUpMhLfDZSavUY.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/_T6K9QyLdRE",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Shotgun Wedding",
                    Genre = "Comedy",
                    DurationMinutes = 100,
                    ReleaseDate = new DateTime(2022, 12, 28),
                    Description = "Darcy and Tom gather their families for the ultimate destination wedding but when the entire party is taken hostage, “’Til Death Do Us Part” takes on a whole new meaning in this hilarious, adrenaline-fueled adventure as Darcy and Tom must save their loved ones—if they don’t kill each other first.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/t79ozwWnwekO0ADIzsFP1E5SkvR.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/UnDk17vHsmc",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Queens on the Run",
                    Genre = "Comedy",
                    DurationMinutes = 97,
                    ReleaseDate = new DateTime(2023, 4, 14),
                    Description = "When four women finally take the road trip they planned in high school, they have no idea of the things they'll bump into sometimes literally.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/oUmuwUIofGsgOr05kieD3Q8ELEO.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/S2rC42wXJ9s",
                    IsActive = true
                },
                new Movie
                {
                    Title = "Sisu",
                    Genre = "Action",
                    DurationMinutes = 91,
                    ReleaseDate = new DateTime(2023, 1, 27),
                    Description = "Deep in the wilderness of Lapland, Aatami Korpi is searching for gold but after he stumbles upon Nazi patrol, a breathtaking and gold-hungry chase through the destroyed and mined Lapland wilderness begins.",
                    PosterUrl = "https://image.tmdb.org/t/p/w500/dHx5yuBb05U9vNaNhIBD7jWyxPk.jpg",
                    TrailerUrl = "https://www.youtube.com/embed/d2k4QA195SU",
                    IsActive = true
                }
            };
            context.Movies.AddRange(movies);
            await context.SaveChangesAsync();

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
                        Status = (BookingStatus)rand.Next(0, 3) // 0: Pending, 1: Confirmed, 2: Cancelled
                    };
                    context.Bookings.Add(booking);
                    await context.SaveChangesAsync();

                    // Add 2 random seats to this booking
                    var hallSeats = allSeats.Where(s => s.HallId == screening.HallId).ToList();
                    if (hallSeats.Count >= 2)
                    {
                        for (int s = 0; s < 2; s++)
                        {
                            var seat = hallSeats[rand.Next(hallSeats.Count)];
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