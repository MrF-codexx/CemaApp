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
            var now = DateTime.UtcNow;
            var newHold = new SeatHold(sessionId, now.AddMinutes(LOCK_MINUTES));

            while (true)
            {
                if (_cache.TryGetValue(key, out var existingHold))
                {
                    // If it is expired, we can replace it.
                    if (existingHold.ExpiresAt < now)
                    {
                        if (_cache.TryUpdate(key, newHold, existingHold))
                        {
                            return true;
                        }
                        // TryUpdate failed (concurrency race), retry
                        continue;
                    }

                    // If it is not expired and held by the SAME session, renew/keep it.
                    if (existingHold.SessionId == sessionId)
                    {
                        if (_cache.TryUpdate(key, newHold, existingHold))
                        {
                            return true;
                        }
                        continue;
                    }

                    // Held by someone else and not expired
                    return false;
                }
                else
                {
                    // No hold exists, try to add
                    if (_cache.TryAdd(key, newHold))
                    {
                        return true;
                    }
                    // TryAdd failed, someone else just added it, retry
                }
            }
        }

        public bool Release(int screeningId, int seatId, string sessionId)
        {
            var key = $"{screeningId}_{seatId}";
            if (_cache.TryGetValue(key, out var hold) && hold.SessionId == sessionId)
            {
                var collection = (ICollection<KeyValuePair<string, SeatHold>>)_cache;
                return collection.Remove(new KeyValuePair<string, SeatHold>(key, hold));
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
