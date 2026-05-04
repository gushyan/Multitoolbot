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
            _httpClient.BaseAddress = new Uri("https://map.gortransperm.ru/json/");
        }

        public async Task<List<ExtStopPlace>> GetAllStopsAsync(CancellationToken ct)
        {
            return await _httpClient.GetFromJsonAsync<List<ExtStopPlace>>("stops", ct) ?? new();
        }
    }
}
