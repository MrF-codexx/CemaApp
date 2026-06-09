using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CemaApp.Models;
using CemaApp.Services;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IBookingService _bookingService;

        public DashboardController(AppDbContext context, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager, IBookingService bookingService)
        {
            _context = context;
            _userManager = userManager;
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new CemaApp.ViewModels.DashboardViewModel();

            // 1. Stats
            viewModel.TotalRevenue = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .SumAsync(b => b.TotalPrice);

            viewModel.TotalBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Confirmed);

            viewModel.TotalUsers = await _userManager.Users.CountAsync();

            // 2. Most Popular Movie
            var popularMovieId = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .GroupBy(b => b.Screening.MovieId)
                .OrderByDescending(g => g.Sum(b => b.TotalPrice))
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            if (popularMovieId != 0)
            {
                viewModel.MostPopularMovie = await _context.Movies.FindAsync(popularMovieId);
                viewModel.PopularMovieBookingCount = await _context.Bookings
                    .CountAsync(b => b.Screening.MovieId == popularMovieId && b.Status == BookingStatus.Confirmed);
            }

            // 3. Average Occupancy Rate
            var totalBookedSeats = await _context.BookingSeats
                .CountAsync(bs => bs.Booking.Status == BookingStatus.Confirmed);

            var screenings = await _context.Screenings.Include(s => s.Hall).ToListAsync();
            var totalCapacity = screenings.Sum(s => s.Hall.TotalRows * s.Hall.SeatsPerRow);

            viewModel.AverageOccupancyRate = totalCapacity > 0 
                ? (double)totalBookedSeats / totalCapacity * 100 
                : 0;

            // 4. Recent Bookings
            viewModel.RecentBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Movie)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .Select(b => new CemaApp.ViewModels.RecentBookingViewModel
                {
                    BookingId = b.Id,
                    UserEmail = b.User.Email,
                    MovieTitle = b.Screening.Movie.Title,
                    BookingDate = b.BookingDate,
                    Amount = b.TotalPrice,
                    Status = b.Status
                })
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<IActionResult> AllBookings(string? searchMovieName, string? filterStatus, string? sortBy, int page = 1)
        {
            await _bookingService.CleanExpiredPendingBookingsAsync();

            var pageSize = 10;
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Screening)
                    .ThenInclude(s => s.Movie)
                .AsQueryable();

            // 1. Search by Movie Name
            if (!string.IsNullOrEmpty(searchMovieName))
            {
                var trimmedSearch = searchMovieName.Trim();
                query = query.Where(b => b.Screening.Movie.Title.Contains(trimmedSearch));
            }

            // 2. Filter by Status
            if (!string.IsNullOrEmpty(filterStatus))
            {
                if (Enum.TryParse<BookingStatus>(filterStatus, out var statusEnum))
                {
                    if (statusEnum == BookingStatus.Cancelled)
                    {
                        query = query.Where(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.RemovedByUser);
                    }
                    else
                    {
                        query = query.Where(b => b.Status == statusEnum);
                    }
                }
            }

            // 3. Sorting
            query = sortBy switch
            {
                "DateAsc" => query.OrderBy(b => b.BookingDate),
                "TitleAsc" => query.OrderBy(b => b.Screening.Movie.Title),
                "TitleDesc" => query.OrderByDescending(b => b.Screening.Movie.Title),
                "PriceAsc" => query.OrderBy(b => b.TotalPrice),
                "PriceDesc" => query.OrderByDescending(b => b.TotalPrice),
                "DateDesc" => query.OrderByDescending(b => b.BookingDate),
                _ => query.OrderByDescending(b => b.BookingDate) // default
            };

            // 4. Pagination
            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CemaApp.ViewModels.AdminBookingListViewModel
            {
                Bookings = bookings,
                SearchMovieName = searchMovieName,
                FilterStatus = filterStatus,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                PageSize = pageSize
            };

            return View(viewModel);
        }
    }
}