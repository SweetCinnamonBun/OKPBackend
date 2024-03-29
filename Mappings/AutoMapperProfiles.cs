using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using OKPBackend.Models.Domain;
using OKPBackend.Models.DTO.Favorites;
using OKPBackend.Models.DTO.Users;

namespace OKPBackend.Mappings
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<User, UserLoginDto>().ReverseMap();
            CreateMap<User, UserRegisterDto>().ReverseMap();


            CreateMap<Favorite, AddFavoriteDto>().ReverseMap();
            CreateMap<Favorite, FavoriteDto>().ReverseMap();
            CreateMap<Favorite, FavoriteDto2>().ReverseMap();
        }
    }
}