using InventoryX.Domain.Models.Tenancy;

namespace InventoryX.Application.Validators.Selling;

public static class DiscountPolicyValidator
{
    public static string? ResolveAuthorizer(
        decimal grossLineAmount,
        decimal discountAmount,
        Role? role,
        string? cashierId,
        string? managerAuthorizer)
    {
        if (discountAmount < 0)
            throw new FluentValidation.ValidationException("Line discount cannot be negative.");
        if (discountAmount == 0) return null;
        if (grossLineAmount <= 0 || discountAmount > grossLineAmount)
            throw new FluentValidation.ValidationException("Line discount cannot exceed the line amount.");

        var discountPercent = discountAmount / grossLineAmount * 100m;
        var cap = role?.MaxDiscountPercent ?? 0m;
        if (discountPercent > cap && string.IsNullOrWhiteSpace(managerAuthorizer))
            throw new FluentValidation.ValidationException(
                $"The {discountPercent:0.##}% discount exceeds the {cap:0.##}% role cap and requires manager authorization.");

        return string.IsNullOrWhiteSpace(managerAuthorizer) ? cashierId ?? "unknown" : managerAuthorizer;
    }
}
