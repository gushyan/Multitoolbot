using Application.Dto;
using AutoMapper;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using System.Data;

namespace Services.Classes
{
    public class FavStopsService : IFavStopsService
    {
        private readonly BusDbContext _dbcontext;
        private readonly IMapper _mapper;

        public FavStopsService(BusDbContext dbcontext, IMapper mapper)
        {
            _dbcontext = dbcontext;
            _mapper = mapper;
        }

        public async Task AddFavoriteStopsByChatIdAsync(FavStopsAddRequest addRequest, CancellationToken ct)
        {
            if (await _dbcontext.FavStops.AnyAsync(f => f.ChatId == addRequest.ChatId && f.StopId == addRequest.StopId, ct))
                throw new DuplicateNameException("Эта остановка уже в избранном");

            _dbcontext.FavStops.Add(_mapper.Map<FavStops>(addRequest));
            await _dbcontext.SaveChangesAsync(ct);
        }

        public async Task DeleteFavoriteStopsByChatIdAsync(FavStopsDeleteRequest deleteRequest, CancellationToken ct)
        {
            var countLines = await _dbcontext.FavStops
                .Where(f => f.ChatId == deleteRequest.ChatId && f.StopId == deleteRequest.StopId)
                .ExecuteDeleteAsync(ct);

            if (countLines == 0)
                throw new KeyNotFoundException("В избранном нет такой остановки");
        }

        public async Task<FavStopsResponse> GetFavoriteStopsByChatIdAsync(long chatId, CancellationToken ct)
        {
            var favStops = await _dbcontext.FavStops
                .AsNoTracking()
                .Where(f => f.ChatId == chatId)
                .Select(f => f.StopId)
                .ToListAsync(ct);

            if (favStops.Count == 0)
                throw new KeyNotFoundException("В избранном нет остановок");

            return new FavStopsResponse(chatId, favStops);
        }

        public async Task<bool> CheckIsFavouriteStops(FavExistRequest favExistRequest, CancellationToken ct) 
        {
            return await _dbcontext.FavStops.AnyAsync(f => f.ChatId == favExistRequest.ChatId && f.StopId == favExistRequest.StopId );
        }
    }
}
