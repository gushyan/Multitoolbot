using Domain.Entities;
using PermGorTrans.ApiClient.Models;

namespace Multitoolbot.Cache
{
    public interface IStopPlaceCache
    {
        IReadOnlyList<ExtStopPlace> Stops { get; }

        Task InitializeAsync(CancellationToken ct);
    }
}
