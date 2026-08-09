using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Purchasing;

public sealed class RecordSupplierInvoiceCommandHandler(IAppDbContext context)
    : IRequestHandler<RecordSupplierInvoiceCommand, SupplierInvoiceDto>
{
    public async Task<SupplierInvoiceDto> Handle(RecordSupplierInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
            throw new FluentValidation.ValidationException("At least one invoice line is required.");
        if (!await context.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException("Supplier not found.");

        // Load PO lines for variance comparison if PO is provided
        Dictionary<Guid, decimal> poLineCosts = [];
        if (request.PurchaseOrderId.HasValue)
        {
            var poLines = await context.PurchaseOrderLines
                .Where(l => l.PurchaseOrderId == request.PurchaseOrderId.Value)
                .ToListAsync(cancellationToken);
            poLineCosts = poLines.ToDictionary(l => l.ProductId, l => l.UnitCost);
        }

        var invoiceLines = request.Lines.Select(input =>
        {
            var orderedCost = poLineCosts.TryGetValue(input.ProductId, out var cost) ? cost : (decimal?)null;
            return new SupplierInvoiceLine
            {
                ProductId = input.ProductId,
                VariantId = input.VariantId,
                Qty = input.Qty,
                UnitPrice = input.UnitPrice,
                OrderedUnitCost = orderedCost,
            };
        }).ToList();

        var invoice = new SupplierInvoice
        {
            SupplierId = request.SupplierId,
            PurchaseOrderId = request.PurchaseOrderId,
            InvoiceNumber = request.InvoiceNumber.Trim(),
            InvoiceDate = request.InvoiceDate,
            HasPriceVariance = invoiceLines.Any(l => l.HasVariance),
            Notes = request.Notes,
            Lines = invoiceLines,
        };

        context.SupplierInvoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);
        return MapDto(invoice);
    }

    internal static SupplierInvoiceDto MapDto(SupplierInvoice invoice) => new(
        invoice.Id, invoice.SupplierId, invoice.PurchaseOrderId, invoice.InvoiceNumber,
        invoice.InvoiceDate, invoice.HasPriceVariance, invoice.Notes,
        invoice.Lines.Select(l => new SupplierInvoiceLineDto(
            l.Id, l.ProductId, l.VariantId, l.Qty, l.UnitPrice, l.OrderedUnitCost, l.HasVariance)).ToList());
}

public sealed class AllocateLandedCostsCommandHandler(IAppDbContext context)
    : IRequestHandler<AllocateLandedCostsCommand, LandedCostAllocationDto>
{
    public async Task<LandedCostAllocationDto> Handle(AllocateLandedCostsCommand request, CancellationToken cancellationToken)
    {
        if (request.TotalAmount <= 0)
            throw new FluentValidation.ValidationException("Landed cost amount must be positive.");

        var receipt = await context.GoodsReceipts
            .Include(r => r.Lines)
            .SingleOrDefaultAsync(r => r.Id == request.GoodsReceiptId, cancellationToken)
            ?? throw new NotFoundException("Goods receipt not found.");

        if (receipt.Lines.Count == 0)
            throw new FluentValidation.ValidationException("Goods receipt has no lines to allocate costs to.");

        // Allocate by value (line value / total value)
        var totalLineValue = receipt.Lines.Sum(l => l.QtyAccepted * l.UnitCost);
        if (totalLineValue <= 0)
            throw new FluentValidation.ValidationException("Cannot allocate landed costs: total receipt value is zero.");

        var allocations = new List<LandedCostLineAllocationDto>();
        foreach (var line in receipt.Lines)
        {
            var lineValue = line.QtyAccepted * line.UnitCost;
            var allocated = totalLineValue > 0 ? request.TotalAmount * (lineValue / totalLineValue) : 0;
            var newUnitCost = line.QtyAccepted > 0
                ? line.UnitCost + (allocated / line.QtyAccepted)
                : line.UnitCost;

            // Update the unit cost on the receipt line
            line.UnitCost = newUnitCost;

            allocations.Add(new LandedCostLineAllocationDto(
                line.Id, line.ProductId, Math.Round(allocated, 4), Math.Round(newUnitCost, 4)));
        }

        await context.SaveChangesAsync(cancellationToken);
        return new LandedCostAllocationDto(receipt.Id, allocations);
    }
}
