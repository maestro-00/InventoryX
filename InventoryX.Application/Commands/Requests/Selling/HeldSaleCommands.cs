using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Selling;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Selling
{
    public sealed record CompleteHeldSaleLineDto(
        Guid Id, decimal Qty, decimal LineDiscount, string? Note, string? DiscountAuthorizedBy);

    public class CompleteHeldSaleCommand : IRequest<SaleDto>, ITenantWriteCommand
    {
        public Guid SaleId { get; init; }
        public List<CreateSalePaymentDto> Payments { get; init; } = [];
        public List<CompleteHeldSaleLineDto>? Lines { get; init; }
    }

    public class UpsertFavouritesLayoutCommand : IRequest<FavouritesLayoutDto>, ITenantWriteCommand
    {
        public Guid RegisterId { get; init; }
        public string LayoutJson { get; init; } = "{}";
    }
}
