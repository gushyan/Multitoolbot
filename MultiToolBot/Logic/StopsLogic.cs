using FuzzySharp;
using Services.Cache;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;
using System.Text;

namespace Multitoolbot.Logic
{
    public class StopsLogic : IStopsLogic
    {
        private readonly IStopPlaceCache _cache;

        public StopsLogic(IStopPlaceCache cache)
        {
            _cache = cache;
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

            return stops.GroupBy(s => s.Name.Replace(". ", "."),StringComparer.OrdinalIgnoreCase).ToList();
        }

        public string FormatArrivalMessage(ArrivalResponse response, string stopName, string note)
        {
            if (response?.RouteTypes == null || response.RouteTypes.Count == 0)
            {
                return $" На остановке {stopName} в ближайшее время транспорта не ожидается.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($" *Остановка*: {stopName}\n"
                + $"*Направление:* {note}\n");

            foreach (var type in response.RouteTypes)
            {
                sb.AppendLine($"*{type.RouteTypeName}*");

                foreach (var route in type.Routes)
                {
                    var arrivals = new List<string>();
                    foreach (var vehicle in route.Vehicles)
                    {
                        string timeOnly = vehicle.ArrivalTime.Length >= 5
                            ? vehicle.ArrivalTime.Substring(0, 5)
                            : vehicle.ArrivalTime;

                        string timeStr;
                        if (vehicle.ArrivalMinutes == 0) timeStr = "прибывает";
                        else if (vehicle.ArrivalMinutes < 0) timeStr = "прибудет в";
                        else
                        {
                            var hours = vehicle.ArrivalMinutes / 60;
                            if (hours > 0)
                                timeStr = $" {hours} ч {vehicle.ArrivalMinutes - hours * 60} мин";
                            else
                                timeStr = $"{vehicle.ArrivalMinutes} мин";

                        }

                        arrivals.Add($"{timeStr} ({timeOnly})");
                    }

                    sb.AppendLine($" *{route.RouteNumber}*: {string.Join(", ", arrivals)}");
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}
