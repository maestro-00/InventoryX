using InventoryX.Application.Commands.Requests.Tenancy;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Tenancy
{
    public class RegisterTenantCommandHandler(
        IAppDbContext context,
        UserManager<User> userManager,
        ITokenService tokenService,
        ITenantContext tenantContext) : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
    {
        private const string InitialChecklist =
            """{"createLocation":false,"addProducts":false,"openingStock":false,"inviteUsers":false,"firstSale":false}""";

        public async Task<RegisterTenantResult> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
        {
            if (await userManager.FindByEmailAsync(request.Email) is not null)
                throw new ConflictException("An account with this email already exists.");

            if (!Enum.TryParse<BusinessType>(request.BusinessType, ignoreCase: true, out var businessType))
                businessType = BusinessType.Other;

            var tenant = new Tenant
            {
                Name = request.BusinessName,
                Country = string.IsNullOrWhiteSpace(request.Country) ? "GH" : request.Country.ToUpperInvariant(),
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "GHS" : request.Currency.ToUpperInvariant(),
                BusinessType = businessType,
                OnboardingChecklist = InitialChecklist,
                // FR-001 business-type defaults: food/pharmacy need expiry capture on receipt
                RequireExpiryOnBatchReceipt = businessType is BusinessType.Food or BusinessType.Pharmacy,
                BillingEmail = request.Email,
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync(cancellationToken);

            var ownerRole = await context.AppRoles.FirstOrDefaultAsync(r => r.Name == "Owner", cancellationToken);
            var owner = new User
            {
                UserName = request.Email,
                Email = request.Email,
                TenantId = tenant.Id,
                IsOwner = true,
                RoleId = ownerRole?.Id,
                LocationScope = "*",
                Name = request.BusinessName,
            };
            var createResult = await userManager.CreateAsync(owner, request.Password);
            if (!createResult.Succeeded)
                throw new FluentValidation.ValidationException(
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));

            var professionalPlan = await context.PlanDefinitions
                .FirstOrDefaultAsync(p => p.Tier == PlanTier.Professional && p.IsActive, cancellationToken)
                ?? throw new ConflictException("Plan catalogue is not seeded.");

            var now = DateTime.UtcNow;
            context.Subscriptions.Add(new Subscription
            {
                TenantId = tenant.Id,
                PlanDefinitionId = professionalPlan.Id,
                Status = SubscriptionStatus.Trialing,
                TrialEndsAt = now.AddDays(14),
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now.AddDays(14),
            });
            await context.SaveChangesAsync(cancellationToken);

            tenantContext.TenantId = tenant.Id;
            tenantContext.UserId = owner.Id;
            tenantContext.Role = ownerRole?.Name;

            var tokens = tokenService.CreateTokenPair(owner, ownerRole);
            return new RegisterTenantResult(
                tenant.Id, tenant.Name, nameof(SubscriptionStatus.Trialing),
                tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.RefreshToken);
        }
    }
}
