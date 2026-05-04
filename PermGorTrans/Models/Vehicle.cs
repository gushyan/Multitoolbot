using System.Text.Json.Serialization;

namespace PermGorTrans.ApiClient.Models
{
    public class Vehicle
    {
        [JsonPropertyName("arrivalTime")]
        public string ArrivalTime { get; set; }

        [JsonPropertyName("arrivalMinutes")]
        public int ArrivalMinutes { get; set; }
    }

}
