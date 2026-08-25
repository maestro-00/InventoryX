using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Purchasing;

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    int LeadTimeDays = 0,
    byte[]? RowVersion = null);

public sealed record CreateSupplierCommand(
    string Name,
    string? Email,
    string? Phone,
    string? Address = null,
    string? Currency = null,
    int LeadTimeDays = 0,
    string? PaymentTerms = null) : IRequest<SupplierDto>, ITenantWriteCommand;

public sealed class UpdateSupplierCommand : IRequest<SupplierDto>, ITenantWriteCommand
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Currency { get; init; }
    public int? LeadTimeDays { get; init; }
    public string? PaymentTerms { get; init; }
    public byte[]? ExpectedRowVersion { get; init; }
}
