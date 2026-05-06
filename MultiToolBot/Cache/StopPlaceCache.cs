using Domain.Entities;
using Microsoft.Extensions.Logging;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Multitoolbot.Cache
{
    public class StopPlaceCache : IStopPlaceCache
    {
        private List<ExtStopPlace> _stopPlaces = new ();

        private bool _isInitialized;

        private readonly ILogger<StopPlaceCache> _logger;

        private readonly IPermGortransClient _client;

        public IReadOnlyList<ExtStopPlace> Stops { get => _stopPlaces.AsReadOnly(); }

        public StopPlaceCache(ILogger<StopPlaceCache> logger, IPermGortransClient permGortransClient) 
        {
            _logger = logger;
            _client = permGortransClient;
        }

        public async Task InitializeAsync(CancellationToken ct)
        {
            if (_isInitialized)
                return;

            _logger.LogDebug("Подтягивание информации о остановках");
            _stopPlaces = await _client.GetAllStopsAsync(ct);
        }
    }
}
