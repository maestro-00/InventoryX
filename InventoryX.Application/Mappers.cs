using AutoMapper;
using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.DTOs.InventoryItemTypes;
using InventoryX.Application.DTOs.Purchases;
using InventoryX.Application.DTOs.RetailStock;
using InventoryX.Application.DTOs.Sales;
using InventoryX.Application.DTOs.Users;
using InventoryX.Domain.Models;

namespace InventoryX.Application
{
    public class Mappers : Profile
    {
        public Mappers()
        {
            CreateMap<InventoryItemCommandDto, InventoryItem>().ReverseMap();
            CreateMap<InventoryItemTypeCommandDto, InventoryItemType>().ReverseMap();
            CreateMap<PurchaseCommandDto, Purchase>().ReverseMap();
            CreateMap<SaleCommandDto, Sale>().ReverseMap();
            CreateMap<GetInventoryItemTypeDto, InventoryItemType>().ReverseMap();
            CreateMap<GetInventoryItemDto, InventoryItem>()
                .ForPath(a => a.Type, o => o.MapFrom(dto => dto.Type))
                .ReverseMap();
            CreateMap<GetPurchaseDto, Purchase>()
                .ForPath(a => a.InventoryItem, o => o.MapFrom(dto => dto.InventoryItem))
                .ForPath(a => a.Purchaser, o => o.MapFrom(dto => dto.Purchaser))
                .ReverseMap();
            CreateMap<GetSaleDto, Sale>()
                .ForPath(a => a.InventoryItem, o => o.MapFrom(dto => dto.InventoryItem))
                .ForPath(a => a.Seller, o => o.MapFrom(dto => dto.Seller))
                .ReverseMap();
            CreateMap<GetUserDto, User>().ReverseMap();
            CreateMap<RetailStock, RetailStockDto>().ReverseMap();
            CreateMap<RetailStock, RetailStockCommandDto>().ReverseMap();
        }
    }
}
