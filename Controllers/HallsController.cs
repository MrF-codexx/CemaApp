using CemaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HallsController : Controller
    {
        private readonly AppDbContext _context;

        public HallsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Halls
        public async Task<IActionResult> Index()
        {
            return View(await _context.Halls.Include(h => h.Seats).ToListAsync());
        }

        // GET: Halls/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Halls/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Hall hall)
        {
            if (ModelState.IsValid)
            {
                _context.Add(hall);
                await _context.SaveChangesAsync();

                // AUTOMATIC SEAT GENERATION
                // For a hall with 10 rows and 10 seats per row, create 100 seat records.
                var seats = new List<Seat>();
                for (int row = 1; row <= hall.TotalRows; row++)
                {
                    string rowLetter = ((char)('A' + row - 1)).ToString();
                    for (int num = 1; num <= hall.SeatsPerRow; num++)
                    {
                        seats.Add(new Seat
                        {
                            HallId = hall.Id,
                            Row = rowLetter,
                            Number = num
                        });
                    }
                }

                _context.Seats.AddRange(seats);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(hall);
        }

        // GET: Halls/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var hall = await _context.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hall == null) return NotFound();

            return View(hall);
        }

        // POST: Halls/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hall = await _context.Halls.FindAsync(id);
            if (hall != null)
            {
                _context.Halls.Remove(hall);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
