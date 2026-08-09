using FluentValidation;
using InventoryX.Application.Commands.Requests.Catalog;

namespace InventoryX.Application.Validators.Catalog
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(300);
            RuleFor(c => c.SellingPrice).GreaterThanOrEqualTo(0);
            RuleFor(c => c.CostPrice).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Sku).MaximumLength(100);
            RuleFor(c => c.Barcode).MaximumLength(100);
        }
    }

    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(c => c.Id).NotEmpty();
            RuleFor(c => c.SellingPrice).GreaterThanOrEqualTo(0).When(c => c.SellingPrice.HasValue);
            RuleFor(c => c.CostPrice).GreaterThanOrEqualTo(0).When(c => c.CostPrice.HasValue);
        }
    }

    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        }
    }
}
