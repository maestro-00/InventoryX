using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Selling
{
    public class CreateRegisterCommand : IRequest<RegisterDto>, IPlanLimitedCommand
    {
        public Guid LocationId { get; init; }
        public required string Name { get; init; }

        public UsageMetric Metric => UsageMetric.Registers;
    }

    public sealed class UpdateRegisterCommand : IRequest<RegisterDto>, ITenantWriteCommand
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public bool? IsActive { get; init; }
        /// <summary>Optional optimistic-concurrency token from If-Match.</summary>
        public byte[]? ExpectedRowVersion { get; init; }
    }

    /// <summary>Opens a shift with a counted opening float; one open shift per register (T041).</summary>
    public class OpenShiftCommand : IRequest<ShiftDto>, ITenantWriteCommand
    {
        public Guid RegisterId { get; init; }
        public decimal OpeningFloat { get; init; }
    }

    public sealed class RecordCashMovementCommand : IRequest<CashMovementDto>, ITenantWriteCommand
    {
        public Guid ShiftId { get; init; }
        public string Type { get; init; } = "CashOut";
        public decimal Amount { get; init; }
        public required string Reason { get; init; }
    }

    public sealed class CloseShiftCommand : IRequest<ShiftDto>, ITenantWriteCommand
    {
        public Guid ShiftId { get; init; }
        public decimal? ClosingCounted { get; init; }
    }
}
