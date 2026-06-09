using System;

namespace CemaApp.Services
{
    public class SeatAlreadyBookedException : Exception
    {
        public SeatAlreadyBookedException(string message) : base(message) { }
    }
}
