using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Inventory;
using InventoryX.Application.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Queries.RequestHandlers.Inventory;

public sealed class GetBatchTraceQueryHandler(IAppDbContext context) : IRequestHandler<GetBatchTraceQuery, BatchTraceDto>
{
    public async Task<BatchTraceDto> Handle(GetBatchTraceQuery request, CancellationToken cancellationToken)
    {
        var batch = await context.Batches.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.BatchId, cancellationToken)
            ?? throw new NotFoundException("Batch not found.");
        BatchTraceSupplierDto? supplier = null;
        if (batch.SupplierId is Guid supplierId)
            supplier = await context.Suppliers.AsNoTracking().Where(item => item.Id == supplierId)
                .Select(item => new BatchTraceSupplierDto(item.Id, item.Name, item.Email, item.Phone))
                .SingleOrDefaultAsync(cancellationToken);
        var receipts = await context.GoodsReceiptLines.AsNoTracking().Where(line => line.BatchId == batch.Id)
            .Join(context.GoodsReceipts.AsNoTracking(), line => line.GoodsReceiptId, receipt => receipt.Id,
                (line, receipt) => new { line, receipt })
            .OrderBy(item => item.receipt.ReceivedAt)
            .Select(item => new BatchTraceReceiptDto(item.receipt.Id, item.receipt.ReceiptNumber, item.receipt.ReceivedAt,
                item.line.QtyReceived, item.line.QtyDamaged, item.receipt.LocationId)).ToListAsync(cancellationToken);
        var sales = await context.SaleLines.AsNoTracking().Where(line => line.BatchId == batch.Id)
            .Join(context.Sales.AsNoTracking(), line => line.SaleId, sale => sale.Id,
                (line, sale) => new { line, sale })
            .OrderBy(item => item.sale.OccurredAt)
            .Select(item => new BatchTraceSaleDto(item.sale.Id, item.sale.OccurredAt, item.line.Qty, item.sale.CashierId, item.sale.LocationId))
            .ToListAsync(cancellationToken);
        return new BatchTraceDto(batch.Id, batch.BatchNumber, batch.ProductId, batch.VariantId, batch.ExpiresAt,
            supplier, receipts, sales);
    }
}
