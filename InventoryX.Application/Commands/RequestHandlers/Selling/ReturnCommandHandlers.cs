using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Selling;
using InventoryX.Domain.Models.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Selling
{
    public static class ReturnMapping
    {
        public static ReturnTransactionDto ToDto(ReturnTransaction r) => new()
        {
            Id = r.Id,
            OriginalSaleId = r.OriginalSaleId,
            ExchangeSaleId = r.ExchangeSaleId,
            Status = r.Status.ToString(),
            AuthorizationRequired = r.AuthorizationRequired,
            AuthorizedBy = r.AuthorizedBy,
            RefundTender = r.RefundTender.ToString(),
            RefundTotal = r.RefundTotal,
            OccurredAt = r.OccurredAt,
            Reason = r.Reason,
            Lines = r.Lines.Select(l => new ReturnLineDto
            {
                Id = l.Id,
                SaleLineId = l.SaleLineId,
                ProductId = l.ProductId,
                VariantId = l.VariantId,
                Qty = l.Qty,
                OriginalUnitPrice = l.OriginalUnitPrice,
                OriginalTaxAmount = l.OriginalTaxAmount,
                LineRefund = l.LineRefund,
                Disposition = l.Disposition.ToString(),
            }).ToList(),
        };
    }

    /// <summary>Shared return-authorization + line-building logic (T053).</summary>
    public static class ReturnProcessor
    {
        public static async Task<ReturnTransaction> BuildReturnAsync(
            IAppDbContext context,
            Guid originalSaleId,
            IReadOnlyList<CreateReturnLineDto> lineRequests,
            string refundTenderName,
            string? authorizedBy,
            string? reason,
            CancellationToken cancellationToken)
        {
            var sale = await context.Sales
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == originalSaleId, cancellationToken)
                ?? throw new NotFoundException("Original sale not found.");
            if (sale.Status == SaleStatus.Voided)
                throw new ConflictException("Cannot return against a voided sale.");

            if (!Enum.TryParse<RefundTender>(refundTenderName, true, out var refundTender))
                throw new FluentValidation.ValidationException($"Unknown refund tender '{refundTenderName}'.");

            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == sale.TenantId, cancellationToken);

            var ret = new ReturnTransaction
            {
                OriginalSaleId = sale.Id,
                RefundTender = refundTender,
                AuthorizedBy = authorizedBy,
                Reason = reason,
                OccurredAt = DateTime.UtcNow,
            };

            decimal refundTotal = 0;
            foreach (var lineRequest in lineRequests)
            {
                var saleLine = sale.Lines.FirstOrDefault(l => l.Id == lineRequest.SaleLineId)
                    ?? throw new NotFoundException($"Sale line {lineRequest.SaleLineId} not found on the original sale.");
                if (lineRequest.Qty <= 0)
                    throw new FluentValidation.ValidationException("Return quantity must be positive.");
                var returnable = saleLine.Qty - saleLine.QtyReturned;
                if (lineRequest.Qty > returnable)
                    throw new FluentValidation.ValidationException(
                        $"Cannot return {lineRequest.Qty} of '{saleLine.ProductName}'; only {returnable} remain returnable.");
                if (!Enum.TryParse<ReturnDisposition>(lineRequest.Disposition, true, out var disposition))
                    throw new FluentValidation.ValidationException($"Unknown disposition '{lineRequest.Disposition}'.");

                // Original commercial terms reapplied automatically (FR-041): tax was a
                // per-line snapshot, so prorate it across the returned quantity.
                var lineRefund = Math.Round(lineRequest.Qty * saleLine.UnitPrice - Proportional(saleLine.LineDiscount, saleLine.Qty, lineRequest.Qty)
                    + Proportional(saleLine.TaxAmount, saleLine.Qty, lineRequest.Qty), 2);
                var originalTax = Math.Round(Proportional(saleLine.TaxAmount, saleLine.Qty, lineRequest.Qty), 4);

                ret.Lines.Add(new ReturnLine
                {
                    SaleLineId = saleLine.Id,
                    ProductId = saleLine.ProductId,
                    VariantId = saleLine.VariantId,
                    BatchId = saleLine.BatchId,
                    Qty = lineRequest.Qty,
                    OriginalUnitPrice = saleLine.UnitPrice,
                    OriginalTaxAmount = originalTax,
                    LineRefund = lineRefund,
                    Disposition = disposition,
                });
                refundTotal += lineRefund;
            }

            ret.RefundTotal = Math.Round(refundTotal, 2);

            // Authorization gate: above the tenant threshold (or receiptless — no
            // original sale reference is impossible here, so threshold governs) → 423.
            var threshold = tenant?.ReturnAuthorizationThreshold;
            var requiresAuth = threshold is not null && ret.RefundTotal > threshold;
            ret.AuthorizationRequired = requiresAuth;
            if (requiresAuth && string.IsNullOrWhiteSpace(authorizedBy))
                throw new ApprovalRequiredException(
                    $"Refunds above {threshold} require manager authorization.", ret.Id);
            ret.Status = ReturnStatus.Completed;

            return ret;
        }

        private static decimal Proportional(decimal amount, decimal totalQty, decimal partQty)
            => totalQty == 0 ? 0 : amount * (partQty / totalQty);

        /// <summary>Apply the stock + sale-side effects of an authorized return.</summary>
        public static async Task ApplyEffectsAsync(
            IAppDbContext context,
            IStockLedger stockLedger,
            ReturnTransaction ret,
            CancellationToken cancellationToken)
        {
            var sale = ret.OriginalSale!;
            var movements = new List<StockMovementRequest>();
            foreach (var line in ret.Lines)
            {
                // Quarantined stock does not re-enter sellable inventory (FR-041).
                if (line.Disposition == ReturnDisposition.ToStock)
                {
                    movements.Add(new StockMovementRequest(
                        MovementType.ReturnIn,
                        line.ProductId,
                        sale.LocationId,
                        line.Qty,
                        VariantId: line.VariantId,
                        BatchId: line.BatchId,
                        CorrelationId: ret.Id,
                        OccurredAt: ret.OccurredAt));
                }

                var saleLine = sale.Lines.First(l => l.Id == line.SaleLineId);
                saleLine.QtyReturned += line.Qty;
            }

            if (movements.Count > 0)
                await stockLedger.AppendAsync(movements, cancellationToken);

            sale.Status = sale.Lines.All(l => l.QtyReturned >= l.Qty)
                ? SaleStatus.Returned
                : SaleStatus.PartiallyReturned;
        }
    }

    public class CreateReturnCommandHandler(
        IAppDbContext context,
        IStockLedger stockLedger) : IRequestHandler<CreateReturnCommand, ReturnTransactionDto>
    {
        public async Task<ReturnTransactionDto> Handle(CreateReturnCommand request, CancellationToken cancellationToken)
        {
            var ret = await ReturnProcessor.BuildReturnAsync(
                context, request.OriginalSaleId, request.Lines, request.RefundTender,
                request.AuthorizedBy, request.Reason, cancellationToken);

            ret.OriginalSale = await context.Sales.Include(s => s.Lines)
                .FirstAsync(s => s.Id == request.OriginalSaleId, cancellationToken);
            await ReturnProcessor.ApplyEffectsAsync(context, stockLedger, ret, cancellationToken);

            context.ReturnTransactions.Add(ret);
            await context.SaveChangesAsync(cancellationToken);
            return ReturnMapping.ToDto(ret);
        }
    }

    public class CreateExchangeCommandHandler(
        IAppDbContext context,
        IStockLedger stockLedger,
        ISender sender) : IRequestHandler<CreateExchangeCommand, ReturnTransactionDto>
    {
        public async Task<ReturnTransactionDto> Handle(CreateExchangeCommand request, CancellationToken cancellationToken)
        {
            var ret = await ReturnProcessor.BuildReturnAsync(
                context, request.OriginalSaleId, request.Lines, nameof(RefundTender.Original),
                request.AuthorizedBy, request.Reason, cancellationToken);

            // New-sale side: settle only the difference between new goods and the refund.
            var newSale = await sender.Send(new CreateSaleCommand
            {
                ClientSaleId = Guid.NewGuid(),
                RegisterId = request.RegisterId,
                ShiftId = request.ShiftId,
                Status = "Completed",
                Lines = request.NewLines,
                Payments = request.Payments,
            }, cancellationToken);

            ret.ExchangeSaleId = newSale.Id;
            ret.OriginalSale = await context.Sales.Include(s => s.Lines)
                .FirstAsync(s => s.Id == request.OriginalSaleId, cancellationToken);
            await ReturnProcessor.ApplyEffectsAsync(context, stockLedger, ret, cancellationToken);

            context.ReturnTransactions.Add(ret);
            await context.SaveChangesAsync(cancellationToken);
            return ReturnMapping.ToDto(ret);
        }
    }
}
