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

    /// <summary>Opens a shift with a counted opening float; one open shift per register (T041).</summary>
    public class OpenShiftCommand : IRequest<ShiftDto>, ITenantWriteCommand
    {
        public Guid RegisterId { get; init; }
        public decimal OpeningFloat { get; init; }
    }
}
