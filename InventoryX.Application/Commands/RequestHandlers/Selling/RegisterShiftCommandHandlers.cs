using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Selling
{
    public class CreateRegisterCommandHandler(IAppDbContext context) : IRequestHandler<CreateRegisterCommand, RegisterDto>
    {
        public async Task<RegisterDto> Handle(CreateRegisterCommand request, CancellationToken cancellationToken)
        {
            if (!await context.Locations.AnyAsync(l => l.Id == request.LocationId && !l.IsDeleted, cancellationToken))
                throw new NotFoundException("Location not found.");

            var register = new Register { LocationId = request.LocationId, Name = request.Name };
            context.Registers.Add(register);
            await context.SaveChangesAsync(cancellationToken);
            return Map(register);
        }

        internal static RegisterDto Map(Register register) => new()
        {
            Id = register.Id,
            LocationId = register.LocationId,
            Name = register.Name,
            IsActive = register.IsActive,
            RowVersion = register.RowVersion,
        };
    }

    public sealed class UpdateRegisterCommandHandler(IAppDbContext context)
        : IRequestHandler<UpdateRegisterCommand, RegisterDto>
    {
        public async Task<RegisterDto> Handle(UpdateRegisterCommand request, CancellationToken cancellationToken)
        {
            var register = await context.Registers
                .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Register not found.");

            RowVersionGuard.EnsureMatch(register.RowVersion, request.ExpectedRowVersion);

            if (request.Name is not null) register.Name = request.Name;
            if (request.IsActive is not null) register.IsActive = request.IsActive.Value;

            await context.SaveChangesAsync(cancellationToken);
            return CreateRegisterCommandHandler.Map(register);
        }
    }

    public class OpenShiftCommandHandler(IAppDbContext context, ITenantContext tenantContext)
        : IRequestHandler<OpenShiftCommand, ShiftDto>
    {
        public async Task<ShiftDto> Handle(OpenShiftCommand request, CancellationToken cancellationToken)
        {
            var register = await context.Registers
                .FirstOrDefaultAsync(r => r.Id == request.RegisterId && r.IsActive, cancellationToken)
                ?? throw new NotFoundException("Register not found.");

            if (await context.Shifts.AnyAsync(
                    s => s.RegisterId == register.Id && s.Status == ShiftStatus.Open, cancellationToken))
                throw new ConflictException("This register already has an open shift.");

            var shift = new Shift
            {
                RegisterId = register.Id,
                OpenedBy = tenantContext.UserId ?? "unknown",
                OpenedAt = DateTime.UtcNow,
                OpeningFloat = request.OpeningFloat,
            };
            context.Shifts.Add(shift);
            await context.SaveChangesAsync(cancellationToken);
            return new ShiftDto
            {
                Id = shift.Id,
                RegisterId = shift.RegisterId,
                OpenedBy = shift.OpenedBy,
                OpenedAt = shift.OpenedAt,
                OpeningFloat = shift.OpeningFloat,
                Status = shift.Status.ToString(),
            };
        }
    }
}
