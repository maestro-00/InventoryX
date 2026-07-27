using FluentAssertions;
using InventoryX.Application.Commands.RequestHandlers.Catalog;
using InventoryX.Application.Commands.Requests.Catalog;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Validators.Catalog;
using InventoryX.Common.Tests;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Tests.Catalog;

/// <summary>T028 — product/category/variant command handlers and validators.</summary>
public sealed class ProductCommandTests : IDisposable
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private readonly TestDb _db = new(Tenant);

    private static CreateProductCommand NewProduct(string name = "Sugar 1kg", string? sku = "SUG-001") => new()
    {
        Name = name,
        Sku = sku,
        SellingPrice = 10m,
        CostPrice = 6m,
    };

    [Fact]
    public async Task Creates_product_with_defaults()
    {
        await using var context = _db.CreateContext();
        var handler = new CreateProductCommandHandler(context);

        var result = await handler.Handle(NewProduct(), CancellationToken.None);

        var product = await context.Products.SingleAsync();
        product.Name.Should().Be("Sugar 1kg");
        product.Sku.Should().Be("SUG-001");
        result.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task Duplicate_sku_is_rejected()
    {
        await using var context = _db.CreateContext();
        var handler = new CreateProductCommandHandler(context);
        await handler.Handle(NewProduct(), CancellationToken.None);

        var act = () => handler.Handle(NewProduct("Other name"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*SKU*");
    }

    [Fact]
    public void Validator_rejects_missing_name_and_negative_price()
    {
        var validator = new CreateProductCommandValidator();

        var result = validator.Validate(new CreateProductCommand { Name = "", SellingPrice = -1m });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.SellingPrice));
    }

    [Fact]
    public async Task Variant_creation_validates_attribute_schema()
    {
        await using var context = _db.CreateContext();
        var createHandler = new CreateProductCommandHandler(context);
        var created = await createHandler.Handle(new CreateProductCommand
        {
            Name = "T-Shirt",
            SellingPrice = 50m,
            TrackingMode = "Variant",
            VariantAttributes = ["Size", "Colour"],
        }, CancellationToken.None);

        var variantHandler = new AddProductVariantsCommandHandler(context);
        await variantHandler.Handle(new AddProductVariantsCommand
        {
            ProductId = created.Id,
            Variants =
            [
                new VariantInputDto { AttributeValues = new Dictionary<string, string> { ["Size"] = "M", ["Colour"] = "Red" }, Sku = "TS-M-RED" },
            ],
        }, CancellationToken.None);

        (await context.ProductVariants.CountAsync()).Should().Be(1);

        var act = () => variantHandler.Handle(new AddProductVariantsCommand
        {
            ProductId = created.Id,
            Variants = [new VariantInputDto { AttributeValues = new Dictionary<string, string> { ["Flavour"] = "Mint" } }],
        }, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Category_name_must_be_unique_per_parent()
    {
        await using var context = _db.CreateContext();
        var handler = new CreateCategoryCommandHandler(context);
        await handler.Handle(new CreateCategoryCommand { Name = "Drinks" }, CancellationToken.None);

        var act = () => handler.Handle(new CreateCategoryCommand { Name = "Drinks" }, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
