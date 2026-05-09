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
        public async Task<ArrivalResponse> GetArrivalTimesByStops(int stopId, CancellationToken ct)
        {
            return await _permgortransClient.GetArrivalTimesByStops(stopId, ct);
        }
        public List<ExtStopPlace> SearchStops(string text)
        {
            var term = text.ToLower();
            return _cache.Stops
                .Select(stop => new { Stop = stop, Score = Fuzz.PartialRatio(term, stop.Name.ToLower()) })
                .Where(x => x.Score > 75)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Stop)
                .Take(10)
                .ToList();
        }
        public List<IGrouping<string, ExtStopPlace>> SearchGroupStops(string text)
        {
            var stops = SearchStops(text);

            return stops.GroupBy(s => s.Name.Replace(". ", "."), StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

}
