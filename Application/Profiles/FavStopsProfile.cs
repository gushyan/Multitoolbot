using Application.Dto;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Profiles
{
    public class FavStopsProfile : Profile
    {
        public FavStopsProfile() 
        {
            CreateMap<FavStopsResponse, FavStops>().ReverseMap();
            CreateMap<FavStopsAddRequest, FavStops>().ReverseMap();
        }
    }
}
