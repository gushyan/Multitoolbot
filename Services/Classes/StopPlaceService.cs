using Application.Dto;
using AutoMapper;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services.Classes
{
    public class StopPlaceService : IStopPlaceInterface
    {
        private readonly BusDbContext _dbcontext;
        private readonly IMapper _mapper;

        public StopPlaceService(BusDbContext dbcontext, IMapper mapper)
        {
            this._dbcontext = dbcontext;
            this._mapper = mapper;
        }

        public async Task AddStopPlaceAsync(StopPlaceAddRequest addRequest, CancellationToken token)
        {
            if (await _dbcontext.StopPlaces.AnyAsync(sp => sp.Name == addRequest.Name, token))
                throw new ArgumentException($"Остановка {addRequest.Name} уже есть");

            _dbcontext.StopPlaces.Add(_mapper.Map<StopPlace>(addRequest));
            await _dbcontext.SaveChangesAsync(token);
        }

        public async Task DeleteStopPlaceByNameAsync(string name, CancellationToken token)
        {
            if (await _dbcontext.StopPlaces
                .Where(sp => sp.Name == name)
                .ExecuteDeleteAsync(token) == 0)
                throw new KeyNotFoundException($"Остановки {name} нет");
        }

        public async Task<List<string>> GetAllStopPlaces(CancellationToken token)
        {
            return await _dbcontext.StopPlaces.Select(s => s.Name).ToListAsync();
        }

        public async Task<StopPlace> GetStopPlaceByNameAsync(string name, CancellationToken token)
        {
            var stopPlace = await _dbcontext.StopPlaces
                .AsNoTracking()
                .FirstOrDefaultAsync(sp => sp.Name == name, token);

            if (stopPlace == null)
                throw new KeyNotFoundException($"Остановки {name} нет");

            return stopPlace;
        }

        public async Task UpdateStopPlaceNameAsync(StopPlaceUpdateNameRequest updateNameRequest, CancellationToken token)
        {
            if (updateNameRequest.OldName != updateNameRequest.NewName)
            {
                if (await _dbcontext.StopPlaces.AnyAsync(sp => sp.Name == updateNameRequest.NewName, token))
                {
                    throw new ArgumentException($"Остановка {updateNameRequest.NewName} уже есть");
                }
            }

            var affectedRows = await _dbcontext.StopPlaces
                .Where(sp => sp.Name == updateNameRequest.OldName)
                .ExecuteUpdateAsync(s => s.SetProperty(sp => sp.Name, updateNameRequest.NewName), token);

            if (affectedRows == 0)
                throw new KeyNotFoundException($"Остановки {updateNameRequest.OldName} нет");
        }
    }
}
