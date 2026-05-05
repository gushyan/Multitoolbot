using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.Classes
{
    public class StopService:IStopService
    {
        private readonly IPermGortransClient _permgortransClient;

        public StopService(IPermGortransClient permGortransClient)
        {
            _permgortransClient = permGortransClient;
        }
        public async Task<ArrivalResponse> GetArrivalTimesByStops(int stopId, CancellationToken ct)
        {
            return await _permgortransClient.GetArrivalTimesByStops(stopId, ct);
        }
    }

}
