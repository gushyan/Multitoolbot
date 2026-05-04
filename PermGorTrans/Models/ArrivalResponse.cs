using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PermGorTrans.ApiClient.Models
{
    public class ArrivalResponse
    {
        [JsonPropertyName("routeTypes")]
        public List<RouteType> RouteTypes { get; set; } = new();
    }
}
