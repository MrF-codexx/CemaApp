using CemaApp.Models;
using CemaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Controllers
{
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MoviesController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        // Public View: Anyone can see the list of movies with optional filtering
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchString, string? genre, int page = 1)
        {
            var query = _context.Movies.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var trimmedSearch = searchString.Trim();
                query = query.Where(m => m.Title.Contains(trimmedSearch) || m.Description.Contains(trimmedSearch));
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(m => m.Genre == genre);
            }

            // Pagination parameters
            int pageSize = 10;
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var movies = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Get unique genres for the filter dropdown
            ViewBag.Genres = await _context.Movies
                .AsNoTracking()
                .Select(m => m.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentGenre = genre;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(movies);
        }
        // Public View: Anyone can see movie details
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var movie = await _context.Movies
                    .Include(m => m.Screenings)
                    .ThenInclude(s => s.Hall)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (movie == null)
                {
                    return NotFound();
                }
                return View(movie);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DETAILS ERROR] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        // GET: Movies/Create
        // This simply returns the empty form to the Admin
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(MovieCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = null;

                //  Handle the Image Upload if a file was provided
                if (model.PosterImage != null)
                {
                    // Define where to save the image (wwwroot/images/posters)
                    string webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string uploadsFolder = Path.Combine(webRootPath, "images", "posters");

                    Directory.CreateDirectory(uploadsFolder);

                    // Ensure the file name is unique 
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.PosterImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Copy the file to the server
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.PosterImage.CopyToAsync(fileStream);
                    }
                }

                Movie newMovie = new Movie
                {
                    Title = model.Title,
                    Description = model.Description,
                    Genre = model.Genre,
                    DurationMinutes = model.DurationMinutes,
                    ReleaseDate = model.ReleaseDate,
                    PosterUrl = uniqueFileName,
                    TrailerUrl = model.TrailerUrl,
                    IsActive = true
                };

                _context.Movies.Add(newMovie);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }
        // GET: Movies/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            var model = new MovieEditViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Genre = movie.Genre,
                DurationMinutes = movie.DurationMinutes,
                ReleaseDate = movie.ReleaseDate,
                IsActive = movie.IsActive,
                ExistingPosterUrl = movie.PosterUrl,
                TrailerUrl = movie.TrailerUrl
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var movie = await _context.Movies.FindAsync(model.Id);
                if (movie == null) return NotFound();

                if (model.NewPosterImage != null)
                {
                    string webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string uploadsFolder = Path.Combine(webRootPath, "images", "posters");

                    Directory.CreateDirectory(uploadsFolder);

                    if (!string.IsNullOrEmpty(movie.PosterUrl))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, movie.PosterUrl);
                        try
                        {
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                       
                            Console.WriteLine($"Warning: Could not delete old image: {ex.Message}");
                        }
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.NewPosterImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.NewPosterImage.CopyToAsync(fileStream);
                    }

                    movie.PosterUrl = uniqueFileName;
                }

                movie.Title = model.Title;
                movie.Description = model.Description;
                movie.Genre = model.Genre;
                movie.DurationMinutes = model.DurationMinutes;
                movie.ReleaseDate = model.ReleaseDate;
                movie.IsActive = model.IsActive;
                movie.TrailerUrl = model.TrailerUrl;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
        // GET: Movies/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            return View(movie);
        }

        // POST: Movies/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                // Clean up the image file from the server
                if (!string.IsNullOrEmpty(movie.PosterUrl))
                {
                    string webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string filePath = Path.Combine(webRootPath, "images", "posters", movie.PosterUrl);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}