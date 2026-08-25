using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Purchasing;

public sealed class CreateSupplierCommandHandler(IAppDbContext context)
    : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Currency = request.Currency,
            LeadTimeDays = request.LeadTimeDays,
            PaymentTerms = request.PaymentTerms,
        };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken);
        return Map(supplier);
    }

    internal static SupplierDto Map(Supplier supplier) =>
        new(supplier.Id, supplier.Name, supplier.Email, supplier.Phone, supplier.LeadTimeDays, supplier.RowVersion);
}

public sealed class UpdateSupplierCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Supplier not found.");

        RowVersionGuard.EnsureMatch(supplier.RowVersion, request.ExpectedRowVersion);

        if (request.Name is not null) supplier.Name = request.Name;
        if (request.Email is not null) supplier.Email = request.Email;
        if (request.Phone is not null) supplier.Phone = request.Phone;
        if (request.Address is not null) supplier.Address = request.Address;
        if (request.Currency is not null) supplier.Currency = request.Currency;
        if (request.LeadTimeDays is not null) supplier.LeadTimeDays = request.LeadTimeDays.Value;
        if (request.PaymentTerms is not null) supplier.PaymentTerms = request.PaymentTerms;

        await context.SaveChangesAsync(cancellationToken);
        return CreateSupplierCommandHandler.Map(supplier);
    }
}
