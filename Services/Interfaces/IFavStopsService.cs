using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Application.Dto;

namespace Services.Interfaces
{
    public interface IFavStopsService
    {
        public Task AddFavoriteStopsByChatIdAsync(FavStopsAddRequest addRequest, CancellationToken ct);
        public Task DeleteFavoriteStopsByChatIdAsync(FavStopsDeleteRequest deleteRequest, CancellationToken ct);
        public Task<FavStopsResponse> GetFavoriteStopsByChatIdAsync(long chatId, CancellationToken ct);
    }
}
