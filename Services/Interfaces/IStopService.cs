using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.Interfaces
{
    public interface IStopService
    {
        public Task<ArrivalResponse> GetArrivalTimesByStops(int stopId, CancellationToken ct);

    }
}
