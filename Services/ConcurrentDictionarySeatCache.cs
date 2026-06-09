using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CemaApp.Services
{
    public record SeatHold(string SessionId, DateTime ExpiresAt);

    public class ConcurrentDictionarySeatCache : ISeatReservationCache
    {
        private readonly ConcurrentDictionary<string, SeatHold> _cache = new();
        private const int LOCK_MINUTES = 7;

        public bool TryHold(int screeningId, int seatId, string sessionId)
        {
            var key = $"{screeningId}_{seatId}";
            var hold = new SeatHold(sessionId, DateTime.UtcNow.AddMinutes(LOCK_MINUTES));
            return _cache.TryAdd(key, hold);
        }

        public bool Release(int screeningId, int seatId, string sessionId)
        {
            var key = $"{screeningId}_{seatId}";
            if (_cache.TryGetValue(key, out var hold) && hold.SessionId == sessionId)
            {
                return _cache.TryRemove(key, out _);
            }
            return false;
        }

        public void ForceRelease(int screeningId, int seatId)
        {
            var key = $"{screeningId}_{seatId}";
            _cache.TryRemove(key, out _);
        }

        public bool IsHeldBy(int screeningId, int seatId, string sessionId)
        {
            var key = $"{screeningId}_{seatId}";
            if (_cache.TryGetValue(key, out var hold))
            {
                if (hold.ExpiresAt > DateTime.UtcNow && hold.SessionId == sessionId)
                {
                    return true;
                }
            }
            return false;
        }

        public Dictionary<string, SeatHold> GetAllHolds()
        {
            return _cache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache.Where(kvp => kvp.Value.ExpiresAt < now).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
