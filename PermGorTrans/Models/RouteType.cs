using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PermGorTrans.ApiClient.Models
{
    public class RouteType
    {
        [JsonPropertyName("routeTypeName")]
        public string RouteTypeName { get; set; }

        [JsonPropertyName("routes")]
        public List<Route> Routes { get; set; } = new();
    }
}
