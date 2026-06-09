using System.Collections.Generic;

namespace CemaApp.Services
{
    public interface ISeatReservationCache
    {
        bool TryHold(int screeningId, int seatId, string sessionId);
        bool Release(int screeningId, int seatId, string sessionId);
        void ForceRelease(int screeningId, int seatId);
        bool IsHeldBy(int screeningId, int seatId, string sessionId);
        Dictionary<string, SeatHold> GetAllHolds();
        void PurgeExpired();
    }
}
