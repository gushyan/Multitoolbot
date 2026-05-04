using Application.Dto;
using AutoMapper;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using System.Data;

namespace Services.Classes
{
    public class FavStopsService: IFavStopsService
    {
        private readonly BusDbContext _dbcontext;
        private readonly IMapper _mapper;

        public FavStopsService(BusDbContext dbcontext, IMapper mapper)
        {
            this._dbcontext = dbcontext;
            this._mapper = mapper;
        }

        public async Task AddFavoriteStopsByChatIdAsync(FavStopsAddRequest addRequest, CancellationToken ct)
        {
            var fav = await _dbcontext.FavStops.FirstOrDefaultAsync(f => f.ChatId == addRequest.chatId, ct);

            if (fav != null)
            {
                if (fav.StopIds.Contains(addRequest.stopId))
                    throw new DuplicateNameException("Такая остановка уже добавлена");

                fav.StopIds.Add(addRequest.stopId);
                _dbcontext.Entry(fav).Property(f => f.StopIds).IsModified = true;

            }
            else
            {
                FavStops favStops = new FavStops() 
                {
                    ChatId = addRequest.chatId,
                    StopIds = new List<int> { addRequest.stopId }
                    
                };
                _dbcontext.FavStops.Add(favStops);
            }

            await _dbcontext.SaveChangesAsync(ct);
        }

        public async Task DeleteFavoriteStopsByChatIdAsync(FavStopsDeleteRequest deleteRequest, CancellationToken ct) 
        {
            var fav = await _dbcontext.FavStops.FirstOrDefaultAsync(f => f.ChatId == deleteRequest.chatId, ct);
            if (fav == null)
                throw new KeyNotFoundException("У чата нет избранных остановок");

            if (fav.StopIds.Contains(deleteRequest.stopId))
                fav.StopIds.Remove(deleteRequest.stopId);

            else 
                throw new KeyNotFoundException("У чата нет избранных остановок");

            _dbcontext.Entry(fav).Property(f => f.StopIds).IsModified = true;
            await _dbcontext.SaveChangesAsync(ct);
        }

        public async Task<FavStopsResponse> GetFavoriteStopsByChatIdAsync(long chatId, CancellationToken ct) 
        {
            var fav = await _dbcontext.FavStops.FirstOrDefaultAsync(f => f.ChatId == chatId, ct);
            if (fav == null || !fav.StopIds.Any())
                throw new KeyNotFoundException("У чата нет избранных остановок");

            return _mapper.Map<FavStopsResponse>(fav);
        }
    }
}
