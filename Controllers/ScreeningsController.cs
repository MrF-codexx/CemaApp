using CemaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScreeningsController : Controller
    {
        private readonly AppDbContext _context;

        public ScreeningsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Screenings
        public async Task<IActionResult> Index()
        {
            var screenings = await _context.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return View(screenings);
        }

        // GET: Screenings/Create
        public IActionResult Create()
        {
            ViewData["MovieId"] = new SelectList(_context.Movies.Where(m => m.IsActive), "Id", "Title");
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name");
            return View();
        }

        // POST: Screenings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Screening screening)
        {
            if (screening.StartTime < DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Screening date and time cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(screening);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieId"] = new SelectList(_context.Movies.Where(m => m.IsActive), "Id", "Title", screening.MovieId);
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", screening.HallId);
            return View(screening);
        }

        // GET: Screenings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var screening = await _context.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (screening == null) return NotFound();

            return View(screening);
        }

        // POST: Screenings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var screening = await _context.Screenings.FindAsync(id);
            if (screening != null)
            {
                _context.Screenings.Remove(screening);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
