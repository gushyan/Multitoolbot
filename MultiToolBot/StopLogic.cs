using FuzzySharp;
using Multitoolbot.Cache;
using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Multitoolbot
{
    public class StopLogic:IStopLogic
    {
        private readonly IStopPlaceCache _cache;

        public StopLogic(IStopPlaceCache cache) 
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

        public string EditNamesStops(string cleanNote, string stopName) 
        {
            if (!string.IsNullOrEmpty(cleanNote) && cleanNote.Contains(stopName, StringComparison.OrdinalIgnoreCase))        
            {
                cleanNote = cleanNote.Replace(stopName, "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("по ", "", StringComparison.OrdinalIgnoreCase)
                                     .Trim(' ', ',', '(', ')');
            }

            return cleanNote.Replace("в город", "➡️ в город")
                                 .Replace("из города", "⬅️ из города");                                                      
        }

        public string FormatArrivalMessage(ArrivalResponse response, string stopName)
        {
            if (response?.RouteTypes == null || response.RouteTypes.Count == 0)
            {
                return $" На остановке {stopName} в ближайшее время транспорта не ожидается.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($" *Остановка*: {stopName}\n");

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
