using PermGorTrans.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.Interfaces
{
    public interface IStopService
    {
        public Task<ArrivalResponse> GetArrivalTimesByStopsAsync(int stopId, CancellationToken ct);

        public Task<List<ExtStopPlace>> SearchStopsAsync(string text, CancellationToken ct);

        public Task<IReadOnlyList<ExtStopPlace>> GetStops(CancellationToken ct);

        public Task<List<IGrouping<string, ExtStopPlace>>> SearchGroupStops(string text, CancellationToken ct);

    }
}
