using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PermGorTrans.ApiClient
{
    public interface IPermGortransClient
    {
        Task<List<ExtStopPlace>> GetAllStopsAsync(CancellationToken ct);

        Task<ArrivalResponse> GetArrivalTimesByStopsAsync(int id, CancellationToken ct);

    }
}
