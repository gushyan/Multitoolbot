using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Application.Dto;

namespace Services.Interfaces
{
    public interface IStopPlaceInterface
    {
        public Task<StopPlace> GetStopPlaceByNameAsync(string name, CancellationToken token);

        public Task AddStopPlaceAsync(StopPlaceAddRequest addRequest, CancellationToken token);

        public Task DeleteStopPlaceByNameAsync(string name, CancellationToken token);

        public Task UpdateStopPlaceNameAsync(StopPlaceUpdateNameRequest updateNameRequest, CancellationToken token);
    }
}
