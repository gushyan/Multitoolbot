using Domain.Entities;
using PermGorTrans.ApiClient.Models;

namespace Services.Cache
{
    public interface IStopPlaceCache
    {
        IReadOnlyList<ExtStopPlace> Stops { get; }

        Task InitializeAsync(CancellationToken ct);
    }
}
