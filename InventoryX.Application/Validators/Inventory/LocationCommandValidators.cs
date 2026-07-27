using FluentValidation;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Domain.Models.Inventory;

namespace InventoryX.Application.Validators.Inventory;

public sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Kind).Must(value => Enum.TryParse<LocationKind>(value, true, out _))
            .WithMessage("Kind must be Shop, Warehouse, Both, Vehicle, or Stall.");
    }
}

public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Kind).Must(value => Enum.TryParse<LocationKind>(value, true, out _))
            .When(x => x.Kind is not null)
            .WithMessage("Kind must be Shop, Warehouse, Both, Vehicle, or Stall.");
    }
}
