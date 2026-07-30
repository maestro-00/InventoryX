using InventoryX.Application.Behaviors; using InventoryX.Domain.Models.Purchasing; using MediatR;
namespace InventoryX.Application.Commands.Requests.Purchasing;
public sealed record SupplierDto(Guid Id,string Name,string? Email,string? Phone);
public sealed record CreateSupplierCommand(string Name,string? Email,string? Phone) : IRequest<SupplierDto>, ITenantWriteCommand;
