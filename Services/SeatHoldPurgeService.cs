using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CemaApp.Services
{
    public class SeatHoldPurgeService : BackgroundService
    {
        private readonly ISeatReservationCache _cache;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

        public SeatHoldPurgeService(ISeatReservationCache cache)
        {
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_period);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                _cache.PurgeExpired();
            }
        }
    }
}
