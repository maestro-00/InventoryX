using System.Text.Json;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Application.Validators.Selling;
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
        ITaxCalculator taxCalculator,
        ITenantContext tenantContext,
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

            if (request.Lines is not null)
            {
                if (request.Lines.Count != sale.Lines.Count ||
                    request.Lines.Select(line => line.Id).Distinct().Count() != request.Lines.Count)
                    throw new FluentValidation.ValidationException("Edited held sale lines must match the saved sale lines.");

                var linesById = sale.Lines.ToDictionary(line => line.Id);
                var productIds = sale.Lines.Select(line => line.ProductId).Distinct().ToList();
                var products = await context.Products.Include(product => product.TaxTreatment)
                    .Where(product => productIds.Contains(product.Id))
                    .ToDictionaryAsync(product => product.Id, cancellationToken);
                var role = string.IsNullOrWhiteSpace(tenantContext.Role)
                    ? null
                    : await context.AppRoles.FirstOrDefaultAsync(role => role.Name == tenantContext.Role, cancellationToken);

                foreach (var edited in request.Lines)
                {
                    if (!linesById.TryGetValue(edited.Id, out var line) ||
                        !products.TryGetValue(line.ProductId, out var product))
                        throw new FluentValidation.ValidationException("Edited held sale line was not found.");
                    if (edited.Qty <= 0 || (!product.AllowFractional && edited.Qty % 1 != 0))
                        throw new FluentValidation.ValidationException("Enter a valid quantity for the held sale line.");
                    var gross = Math.Round(edited.Qty * line.UnitPrice, 2);
                    line.DiscountAuthorizedBy = DiscountPolicyValidator.ResolveAuthorizer(
                        gross, edited.LineDiscount, role, tenantContext.UserId, edited.DiscountAuthorizedBy);
                    var net = Math.Round(gross - edited.LineDiscount, 4);
                    var components = taxCalculator.Calculate(net, product.TaxTreatment?.ComponentsJson ?? "[]");
                    line.Qty = edited.Qty;
                    line.LineDiscount = edited.LineDiscount;
                    line.Note = edited.Note;
                    line.TaxComponents = JsonSerializer.Serialize(components, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    line.TaxAmount = Math.Round(components.Sum(component => component.Amount), 2);
                    line.LineTotal = Math.Round(net + line.TaxAmount, 2);
                }

                sale.Subtotal = Math.Round(sale.Lines.Sum(line => line.Qty * line.UnitPrice), 2);
                sale.DiscountTotal = sale.Lines.Sum(line => line.LineDiscount);
                sale.TaxTotal = sale.Lines.Sum(line => line.TaxAmount);
                sale.GrandTotal = Math.Round(sale.Subtotal - sale.DiscountTotal + sale.TaxTotal, 2);
            }

            decimal tendered = 0;
            decimal cashTendered = 0;
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
                if (tender == TenderType.Cash) cashTendered += payment.Amount;
            }

            if (tendered < sale.GrandTotal)
                throw new FluentValidation.ValidationException(
                    $"Payments ({tendered}) do not cover the grand total ({sale.GrandTotal}).");
            var overpay = Math.Round(tendered - sale.GrandTotal, 2);
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
