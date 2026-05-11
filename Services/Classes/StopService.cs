using FuzzySharp;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;
using Services.Cache;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.Classes
{
    public class StopService:IStopService
    {
        private readonly IPermGortransClient _permgortransClient;
        private readonly IStopPlaceCache _cache;
        public StopService(IPermGortransClient permGortransClient, IStopPlaceCache stopPlaceCache)
        {
            _permgortransClient = permGortransClient;
            _cache = stopPlaceCache;
        }
        public async Task<ArrivalResponse> GetArrivalTimesByStopsAsync(int stopId, CancellationToken ct)
        {
            return await _permgortransClient.GetArrivalTimesByStopsAsync(stopId, ct);
        }
        public async Task<List<ExtStopPlace>> SearchStopsAsync(string text, CancellationToken ct)
        {
            await _cache.InitializeAsync(ct);

            var term = text.ToLower();
            return _cache.Stops
                .Select(stop => new { Stop = stop, Score = Fuzz.PartialRatio(term, stop.Name.ToLower()) })
                .Where(x => x.Score > 75)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Stop)
                .Take(10)
                .ToList();
        }
        public async Task<List<IGrouping<string, ExtStopPlace>>> SearchGroupStops(string text, CancellationToken ct)
        {
            var stops = await SearchStopsAsync(text, ct);
            return stops.GroupBy(s => s.Name.Replace(". ", "."), StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<IReadOnlyList<ExtStopPlace>> GetStops(CancellationToken ct)
        {
            await _cache.InitializeAsync(ct);
            return _cache.Stops;
        }


    }

}
