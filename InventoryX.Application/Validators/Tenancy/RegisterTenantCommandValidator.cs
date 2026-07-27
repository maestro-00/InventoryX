using FluentValidation;
using InventoryX.Application.Commands.Requests.Tenancy;

namespace InventoryX.Application.Validators.Tenancy
{
    public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
    {
        public RegisterTenantCommandValidator()
        {
            RuleFor(c => c.Email).NotEmpty().EmailAddress();
            RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
            RuleFor(c => c.BusinessName).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Country).Length(2).When(c => !string.IsNullOrEmpty(c.Country));
            RuleFor(c => c.Currency).Length(3).When(c => !string.IsNullOrEmpty(c.Currency));
        }
    }
}
