using CemaApp.Models;
using System.Collections.Generic;

namespace CemaApp.ViewModels
{
    public class AdminBookingListViewModel
    {
        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();
        public string? SearchMovieName { get; set; }
        public string? FilterStatus { get; set; }  
        public string? SortBy { get; set; } 
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; } = 10;
        
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
