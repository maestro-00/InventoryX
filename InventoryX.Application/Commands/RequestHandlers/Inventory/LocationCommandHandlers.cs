using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.DTOs.Inventory;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Inventory
{
    public static class LocationMapping
    {
        public static LocationDto ToDto(Location location) => new()
        {
            Id = location.Id,
            Name = location.Name,
            Address = location.Address,
            Kind = location.Kind.ToString(),
            IsActive = location.IsActive,
        };
    }

    public class CreateLocationCommandHandler(IAppDbContext context) : IRequestHandler<CreateLocationCommand, LocationDto>
    {
        public async Task<LocationDto> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
        {
            if (await context.Locations.AnyAsync(l => l.Name == request.Name && !l.IsDeleted, cancellationToken))
                throw new ConflictException($"A location named '{request.Name}' already exists.");

            var location = new Location
            {
                Name = request.Name,
                Address = request.Address,
                Kind = Enum.TryParse<LocationKind>(request.Kind, true, out var kind) ? kind : LocationKind.Shop,
            };
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);
            return LocationMapping.ToDto(location);
        }
    }

    public class UpdateLocationCommandHandler(IAppDbContext context) : IRequestHandler<UpdateLocationCommand, LocationDto>
    {
        public async Task<LocationDto> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
        {
            var location = await context.Locations
                .FirstOrDefaultAsync(l => l.Id == request.Id && !l.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Location not found.");

            location.Name = request.Name ?? location.Name;
            location.Address = request.Address ?? location.Address;
            if (request.Kind is not null && Enum.TryParse<LocationKind>(request.Kind, true, out var kind))
                location.Kind = kind;
            location.IsActive = request.IsActive ?? location.IsActive;
            await context.SaveChangesAsync(cancellationToken);
            return LocationMapping.ToDto(location);
        }
    }

    public class DeleteLocationCommandHandler(IAppDbContext context) : IRequestHandler<DeleteLocationCommand>
    {
        public async Task Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            var location = await context.Locations
                .FirstOrDefaultAsync(l => l.Id == request.Id && !l.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Location not found.");
            location.IsDeleted = true;
            location.IsActive = false;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
