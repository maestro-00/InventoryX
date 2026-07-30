using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Auth;

public sealed class SetRegisterPinCommandHandler(
    IAppDbContext context, ITenantContext tenantContext, IPasswordHasher<User> passwordHasher)
    : IRequestHandler<SetRegisterPinCommand, bool>
{
    public async Task<bool> Handle(SetRegisterPinCommand request, CancellationToken cancellationToken)
    {
        if (request.Pin.Length is < 4 or > 8 || request.Pin.Any(c => !char.IsDigit(c)))
            throw new FluentValidation.ValidationException("Register PIN must contain 4 to 8 digits.");
        var canManage = tenantContext.Role is "Owner" or "Administrator";
        if (!canManage && tenantContext.UserId != request.UserId)
            throw new CustomException("You may only change your own register PIN.", 403);
        var user = await context.Users.SingleOrDefaultAsync(
            u => u.Id == request.UserId && u.TenantId == tenantContext.TenantId && u.Status == UserStatus.Active,
            cancellationToken) ?? throw new NotFoundException("User not found.");
        var pin = await context.RegisterPins.SingleOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (pin is null)
        {
            pin = new RegisterPin { UserId = user.Id, PasswordHash = string.Empty };
            context.RegisterPins.Add(pin);
        }
        pin.PasswordHash = passwordHasher.HashPassword(user, request.Pin);
        pin.FailedAttempts = 0;
        pin.LockedUntil = null;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ExchangeRegisterPinCommandHandler(
    IAppDbContext context,
    ITenantContext tenantContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService) : IRequestHandler<ExchangeRegisterPinCommand, RegisterPinExchangeResult>
{
    public async Task<RegisterPinExchangeResult> Handle(ExchangeRegisterPinCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(
            u => u.Id == request.UserId && u.TenantId == tenantContext.TenantId && u.Status == UserStatus.Active,
            cancellationToken) ?? throw new NotFoundException("User not found.");
        var register = await context.Registers.SingleOrDefaultAsync(r => r.Id == request.RegisterId && r.IsActive, cancellationToken)
            ?? throw new NotFoundException("Register not found.");
        if (!AllowsLocation(user.LocationScope, register.LocationId))
            throw new CustomException("User is not assigned to the register's location.", 403);
        var pin = await context.RegisterPins.SingleOrDefaultAsync(p => p.UserId == user.Id, cancellationToken)
            ?? throw new FluentValidation.ValidationException("Register PIN is not configured.");
        if (pin.LockedUntil > DateTime.UtcNow) throw new CustomException("Register PIN is temporarily locked.", 423);

        var verification = passwordHasher.VerifyHashedPassword(user, pin.PasswordHash, request.Pin);
        if (verification == PasswordVerificationResult.Failed)
        {
            pin.FailedAttempts++;
            if (pin.FailedAttempts >= 5) pin.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            await context.SaveChangesAsync(cancellationToken);
            throw new FluentValidation.ValidationException("Invalid register PIN.");
        }
        pin.FailedAttempts = 0;
        pin.LockedUntil = null;
        var role = user.RoleId is Guid roleId
            ? await context.AppRoles.SingleOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            : null;
        await context.SaveChangesAsync(cancellationToken);
        return new RegisterPinExchangeResult(tokenService.CreateRegisterScopedToken(user, role, register.Id));
    }

    private static bool AllowsLocation(string? scope, Guid locationId) =>
        scope == "*" || (scope ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => Guid.TryParse(value, out var id) && id == locationId);
}
