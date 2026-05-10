using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace PermGorTrans.ApiClient
{
    public class PermGortransClient: IPermGortransClient
    {
        private readonly HttpClient _httpClient;

        public PermGortransClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ExtStopPlace>> GetAllStopsAsync(CancellationToken ct)
        {
            return await _httpClient.GetFromJsonAsync<List<ExtStopPlace>>("stops", ct) ?? new();
        }

        public async Task<ArrivalResponse> GetArrivalTimesByStopsAsync(int id, CancellationToken ct) 
        {
            return await _httpClient.GetFromJsonAsync<ArrivalResponse>($"arrival-times-vehicles/{id}", ct) ?? new();
        }
    }
}
