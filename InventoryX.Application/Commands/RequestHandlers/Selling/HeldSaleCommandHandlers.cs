using System.Text.Json;
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
    public class CompleteHeldSaleCommandHandler(
        IAppDbContext context,
        IStockLedger stockLedger,
        IPlanEnforcer planEnforcer,
        IPosAccess posAccess,
        IReceiptBuilder? receiptBuilder = null) : IRequestHandler<CompleteHeldSaleCommand, SaleDto>
    {
        public async Task<SaleDto> Handle(CompleteHeldSaleCommand request, CancellationToken cancellationToken)
        {
            var sale = await context.Sales
                .Include(s => s.Lines)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
                ?? throw new NotFoundException("Held sale not found.");
            if (sale.Status != SaleStatus.Held)
                throw new ConflictException("Only held sales can be completed.");
            if (!await posAccess.CanViewOthersAsync(cancellationToken)
                && !string.Equals(sale.CashierId, posAccess.UserId, StringComparison.Ordinal))
                throw new NotFoundException("Held sale not found.");

            var shift = await context.Shifts.FirstOrDefaultAsync(
                s => s.Id == sale.ShiftId && s.RegisterId == sale.RegisterId && s.Status == ShiftStatus.Open,
                cancellationToken)
                ?? throw new ConflictException("Completing a held sale requires its shift to remain open.");
            await posAccess.EnsureCanOperateShiftAsync(shift, cancellationToken);

            decimal tendered = 0;
            foreach (var payment in request.Payments)
            {
                if (!Enum.TryParse<TenderType>(payment.Tender, true, out var tender))
                    throw new FluentValidation.ValidationException($"Unknown tender type '{payment.Tender}'.");
                if (tender is TenderType.StoreCredit or TenderType.GiftCard or TenderType.LoyaltyPoints or TenderType.OnAccount)
                    throw new FluentValidation.ValidationException($"Tender '{tender}' is not available in this cycle.");
                if (payment.Amount <= 0)
                    throw new FluentValidation.ValidationException("Tender amounts must be greater than zero.");
                context.SalePayments.Add(new SalePayment
                {
                    TenantId = sale.TenantId,
                    SaleId = sale.Id,
                    Tender = tender,
                    Amount = payment.Amount,
                    Reference = payment.Reference,
                });
                tendered += payment.Amount;
            }

            if (tendered < sale.GrandTotal)
                throw new FluentValidation.ValidationException(
                    $"Payments ({tendered}) do not cover the grand total ({sale.GrandTotal}).");
            var overpay = Math.Round(tendered - sale.GrandTotal, 2);
            var cashTendered = sale.Payments.Where(p => p.Tender == TenderType.Cash).Sum(p => p.Amount);
            if (overpay > cashTendered)
                throw new FluentValidation.ValidationException("Change cannot exceed the cash portion of the payment.");

            foreach (var line in sale.Lines)
            {
                await stockLedger.AppendAsync([new StockMovementRequest(
                    MovementType.Sale,
                    line.ProductId,
                    sale.LocationId,
                    -line.Qty,
                    VariantId: line.VariantId,
                    BatchId: line.BatchId,
                    CorrelationId: sale.Id,
                    OccurredAt: DateTime.UtcNow)], cancellationToken);
            }

            sale.ChangeGiven = overpay;
            sale.Status = SaleStatus.Completed;
            if (receiptBuilder is not null)
                await receiptBuilder.BuildAsync(sale, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await planEnforcer.IncrementUsageAsync(UsageMetric.SalesThisMonth, cancellationToken: cancellationToken);
            return SaleMapping.ToDto(sale);
        }
    }

    public class UpsertFavouritesLayoutCommandHandler(IAppDbContext context)
        : IRequestHandler<UpsertFavouritesLayoutCommand, FavouritesLayoutDto>
    {
        public async Task<FavouritesLayoutDto> Handle(
            UpsertFavouritesLayoutCommand request,
            CancellationToken cancellationToken)
        {
            if (!await context.Registers.AnyAsync(r => r.Id == request.RegisterId && r.IsActive, cancellationToken))
                throw new NotFoundException("Register not found.");

            try { JsonDocument.Parse(request.LayoutJson); }
            catch (JsonException) { throw new FluentValidation.ValidationException("Favourites layout must be valid JSON."); }

            var layout = await context.FavouritesLayouts
                .FirstOrDefaultAsync(f => f.RegisterId == request.RegisterId, cancellationToken);
            if (layout is null)
            {
                layout = new FavouritesLayout { RegisterId = request.RegisterId };
                context.FavouritesLayouts.Add(layout);
            }
            layout.LayoutJson = request.LayoutJson;
            await context.SaveChangesAsync(cancellationToken);
            return new FavouritesLayoutDto(layout.RegisterId, layout.LayoutJson);
        }
    }
}
