using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PermGorTrans.ApiClient.Models
{
    public class Route
    {
        [JsonPropertyName("routeNumber")]
        public string RouteNumber { get; set; }
        [JsonPropertyName("vehicles")]
        public List<Vehicle> Vehicles { get; set; } = new();
    }
}
