using System;
using System.Collections.Generic;
using System.Text;
using Application.Dto;
using Domain.Entities;
using Services.Interfaces;
using Infrastructure;
using Application.Profiles;
using AutoMapper;

namespace Services.Classes
{
    public class StopPlaceService : IStopPlaceInterface
    {
        private readonly BusDbContext _dbcontext;
        private readonly IMapper _mapper;

        public StopPlaceService (BusDbContext dbcontext, IMapper mapper)
        {
            this._dbcontext = dbcontext;
            this._mapper = mapper;
        }

        public async Task AddStopPlaceAsync(StopPlaceAddRequest addRequest, CancellationToken token)
        {
            _dbcontext.StopPlaces.Add(_mapper.Map<StopPlace>(addRequest));
            await _dbcontext.SaveChangesAsync(token);
        }

        public Task DeleteStopPlaceByNameAsync(string name, CancellationToken token)
        {

        }

        public Task<StopPlace> GetStopPlaceByNameAsync(string name, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task UpdateStopPlaceNameAsync(StopPlaceUpdateNameRequest updateNameRequest, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
