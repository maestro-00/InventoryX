using AutoMapper;
using InventoryX.Application.DTOs.Users;
using InventoryX.Domain.Models;

namespace InventoryX.Application
{
    public class Mappers : Profile
    {
        public Mappers()
        {
            CreateMap<GetUserDto, User>().ReverseMap();
        }
    }
}
