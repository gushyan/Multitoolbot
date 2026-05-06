using Microsoft.Extensions.Logging;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;

namespace Multitoolbot.Cache
{
    public class StopPlaceCache : IStopPlaceCache
    {
        private volatile bool _isInitialized;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly ILogger<StopPlaceCache> _logger;

        private readonly IPermGortransClient _client;

        private IReadOnlyList<ExtStopPlace> stop;

        public IReadOnlyList<ExtStopPlace> Stops { get => stop; }

        public StopPlaceCache(ILogger<StopPlaceCache> logger, IPermGortransClient permGortransClient)
        {
            _logger = logger;
            _client = permGortransClient;
        }

        public async Task InitializeAsync(CancellationToken ct)
        {
            if (_isInitialized)
                return;

            await _semaphore.WaitAsync(ct);

            try
            {
                if (_isInitialized)
                    return;

                _logger.LogDebug("Подтягивание информации о остановках");

                stop = (await _client.GetAllStopsAsync(ct)).AsReadOnly();
                _isInitialized = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
