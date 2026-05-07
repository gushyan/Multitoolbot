using Microsoft.Extensions.Logging;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;

namespace Multitoolbot.Cache
{
    public class StopPlaceCache : IStopPlaceCache
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly ILogger<StopPlaceCache> _logger;

        private readonly IPermGortransClient _client;

        private IReadOnlyList<ExtStopPlace> _stops = Array.Empty<ExtStopPlace>();

        private DateTime _lastPull;

        public IReadOnlyList<ExtStopPlace> Stops { get => _stops; }

        public StopPlaceCache(ILogger<StopPlaceCache> logger, IPermGortransClient permGortransClient)
        {
            _logger = logger;
            _client = permGortransClient;
            _lastPull = new DateTime();
        }

        public async Task InitializeAsync(CancellationToken ct)
        {
            if (Stops.Count!=0 && DateTime.UtcNow.Subtract(_lastPull).TotalDays < 7)
                return;

            await _semaphore.WaitAsync(ct);

            try
            {
                if (Stops.Count != 0 && DateTime.UtcNow.Subtract(_lastPull).TotalDays < 7)
                    return;

                _logger.LogDebug("Подтягивание информации о остановках");

                _stops = (await _client.GetAllStopsAsync(ct)).AsReadOnly();
                _lastPull = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
