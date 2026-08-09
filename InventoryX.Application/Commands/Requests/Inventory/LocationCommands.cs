using InventoryX.Application.Behaviors;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Domain.Models.Tenancy;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Inventory
{
    public class CreateLocationCommand : IRequest<LocationDto>, IPlanLimitedCommand
    {
        public required string Name { get; init; }
        public string? Address { get; init; }
        public string Kind { get; init; } = "Shop";

        public UsageMetric Metric => UsageMetric.Locations;
    }

    public class UpdateLocationCommand : IRequest<LocationDto>, ITenantWriteCommand
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Address { get; init; }
        public string? Kind { get; init; }
        public bool? IsActive { get; init; }
    }

    public class DeleteLocationCommand : IRequest, ITenantWriteCommand
    {
        public Guid Id { get; init; }
    }
}
