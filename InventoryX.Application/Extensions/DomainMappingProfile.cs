using AutoMapper;
using InventoryX.Application.DTOs.Catalog;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Queries.Requests.Tenancy;
using InventoryX.Domain.Models.Catalog;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Extensions
{
    /// <summary>Shared domain-to-contract mappings for the US1 application areas.</summary>
    public sealed class DomainMappingProfile : Profile
    {
        public DomainMappingProfile()
        {
            CreateMap<Tenant, TenantDto>()
                .ForMember(d => d.BusinessType, o => o.MapFrom(s => s.BusinessType.ToString()))
                .ForMember(d => d.ValuationMethod, o => o.MapFrom(s => s.ValuationMethod.ToString()));

            CreateMap<Location, LocationDto>()
                .ForMember(d => d.Kind, o => o.MapFrom(s => s.Kind.ToString()));
            CreateMap<Category, CategoryDto>()
                .ForMember(d => d.Children, o => o.Ignore());
            CreateMap<TaxTreatment, TaxTreatmentDto>();
            CreateMap<ProductVariant, ProductVariantDto>()
                .ForMember(d => d.AttributeValues, o => o.Ignore());

            CreateMap<StockLevel, StockLevelDto>()
                .ForMember(d => d.ProductName, o => o.Ignore());
            CreateMap<StockMovement, StockMovementDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));

            CreateMap<Register, RegisterDto>();
            CreateMap<Shift, ShiftDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
            CreateMap<SaleLine, SaleLineDto>();
            CreateMap<SalePayment, SalePaymentDto>()
                .ForMember(d => d.Tender, o => o.MapFrom(s => s.Tender.ToString()));
        }
    }
}
