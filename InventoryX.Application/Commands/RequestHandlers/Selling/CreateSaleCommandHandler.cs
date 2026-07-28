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
    public static class SaleMapping
    {
        public static SaleDto ToDto(Sale sale) => new()
        {
            Id = sale.Id,
            ClientSaleId = sale.ClientSaleId,
            LocationId = sale.LocationId,
            RegisterId = sale.RegisterId,
            ShiftId = sale.ShiftId,
            CashierId = sale.CashierId,
            Status = sale.Status.ToString(),
            Subtotal = sale.Subtotal,
            DiscountTotal = sale.DiscountTotal,
            TaxTotal = sale.TaxTotal,
            GrandTotal = sale.GrandTotal,
            ChangeDue = sale.ChangeGiven,
            StockConflictFlag = sale.StockConflictFlag,
            OccurredAt = sale.OccurredAt,
            Lines = sale.Lines.Select(l => new SaleLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                VariantId = l.VariantId,
                BatchId = l.BatchId,
                ProductName = l.ProductName,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                LineDiscount = l.LineDiscount,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal,
                TaxComponents = l.TaxComponents,
                Note = l.Note,
            }).ToList(),
            Payments = sale.Payments.Select(p => new SalePaymentDto
            {
                Tender = p.Tender.ToString(),
                Amount = p.Amount,
                Reference = p.Reference,
            }).ToList(),
        };
    }

    public class CreateSaleCommandHandler(
        IAppDbContext context,
        IStockLedger stockLedger,
        ITaxCalculator taxCalculator,
        ITenantContext tenantContext,
        IPlanEnforcer planEnforcer) : IRequestHandler<CreateSaleCommand, SaleDto>
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task<SaleDto> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
        {
            // Idempotent replay: same ClientSaleId returns the original result (research R6)
            var existing = await context.Sales
                .Include(s => s.Lines).Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.ClientSaleId == request.ClientSaleId, cancellationToken);
            if (existing is not null) return SaleMapping.ToDto(existing);

            var register = await context.Registers
                .FirstOrDefaultAsync(r => r.Id == request.RegisterId && r.IsActive, cancellationToken)
                ?? throw new NotFoundException("Register not found.");

            var isHeld = string.Equals(request.Status, "Held", StringComparison.OrdinalIgnoreCase);

            var shift = await context.Shifts.FirstOrDefaultAsync(
                s => s.Id == request.ShiftId && s.RegisterId == register.Id && s.Status == ShiftStatus.Open,
                cancellationToken)
                ?? throw new ConflictException("Sales require an open shift on this register.");

            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await context.Products
                .Include(p => p.TaxTreatment).Include(p => p.Variants)
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, cancellationToken);
            var role = string.IsNullOrWhiteSpace(tenantContext.Role)
                ? null
                : await context.AppRoles.FirstOrDefaultAsync(r => r.Name == tenantContext.Role, cancellationToken);

            var sale = new Sale
            {
                LocationId = register.LocationId,
                RegisterId = register.Id,
                ShiftId = shift.Id,
                CashierId = tenantContext.UserId ?? "unknown",
                ClientSaleId = request.ClientSaleId,
                Channel = SaleChannel.Pos,
                OfflineOrigin = request.OfflineOrigin,
                Status = isHeld ? SaleStatus.Held : SaleStatus.Completed,
                OccurredAt = request.OccurredAt ?? DateTime.UtcNow,
            };

            decimal subtotal = 0, discountTotal = 0, taxTotal = 0;
            foreach (var lineRequest in request.Lines)
            {
                if (!products.TryGetValue(lineRequest.ProductId, out var product))
                    throw new NotFoundException($"Product {lineRequest.ProductId} not found.");
                if (lineRequest.Qty <= 0)
                    throw new FluentValidation.ValidationException("Line quantity must be positive.");
                if (!product.AllowFractional && lineRequest.Qty % 1 != 0)
                    throw new FluentValidation.ValidationException($"'{product.Name}' does not allow fractional quantities.");

                var variant = lineRequest.VariantId is null
                    ? null
                    : product.Variants.FirstOrDefault(v => v.Id == lineRequest.VariantId)
                        ?? throw new NotFoundException($"Variant {lineRequest.VariantId} not found.");

                var unitPrice = lineRequest.UnitPrice ?? variant?.SellingPrice ?? product.SellingPrice;
                var grossLineAmount = lineRequest.Qty * unitPrice;
                var discountAuthorizedBy = DiscountPolicyValidator.ResolveAuthorizer(
                    grossLineAmount,
                    lineRequest.LineDiscount,
                    role,
                    tenantContext.UserId,
                    lineRequest.DiscountAuthorizedBy);
                var net = Math.Round(grossLineAmount - lineRequest.LineDiscount, 4);
                var components = taxCalculator.Calculate(net, product.TaxTreatment?.ComponentsJson ?? "[]");
                var taxAmount = Math.Round(components.Sum(c => c.Amount), 2);

                sale.Lines.Add(new SaleLine
                {
                    ProductId = product.Id,
                    VariantId = variant?.Id,
                    BatchId = lineRequest.BatchId,
                    ProductName = variant is null ? product.Name : $"{product.Name} ({variant.Sku ?? "variant"})",
                    Qty = lineRequest.Qty,
                    UnitPrice = unitPrice,
                    PriceOverridden = lineRequest.UnitPrice is not null,
                    LineDiscount = lineRequest.LineDiscount,
                    DiscountAuthorizedBy = discountAuthorizedBy,
                    TaxComponents = JsonSerializer.Serialize(components, SerializerOptions),
                    TaxAmount = taxAmount,
                    LineTotal = Math.Round(net + taxAmount, 2),
                    Note = lineRequest.Note,
                });

                subtotal += Math.Round(lineRequest.Qty * unitPrice, 2);
                discountTotal += lineRequest.LineDiscount;
                taxTotal += taxAmount;
            }

            sale.Subtotal = Math.Round(subtotal, 2);
            sale.DiscountTotal = Math.Round(discountTotal, 2);
            sale.TaxTotal = Math.Round(taxTotal, 2);
            sale.GrandTotal = Math.Round(subtotal - discountTotal + taxTotal, 2);

            if (!isHeld)
            {
                var tendered = 0m;
                foreach (var payment in request.Payments)
                {
                    if (!Enum.TryParse<TenderType>(payment.Tender, true, out var tender))
                        throw new FluentValidation.ValidationException($"Unknown tender type '{payment.Tender}'.");
                    if (tender is TenderType.StoreCredit or TenderType.GiftCard or TenderType.LoyaltyPoints or TenderType.OnAccount)
                        throw new FluentValidation.ValidationException($"Tender '{tender}' is not available in this cycle.");
                    if (payment.Amount <= 0)
                        throw new FluentValidation.ValidationException("Tender amounts must be greater than zero.");
                    sale.Payments.Add(new SalePayment { Tender = tender, Amount = payment.Amount, Reference = payment.Reference });
                    tendered += payment.Amount;
                }

                if (tendered < sale.GrandTotal)
                    throw new FluentValidation.ValidationException(
                        $"Payments ({tendered}) do not cover the grand total ({sale.GrandTotal}).");

                var overpay = Math.Round(tendered - sale.GrandTotal, 2);
                if (overpay > 0 && !sale.Payments.Any(p => p.Tender == TenderType.Cash))
                    throw new FluentValidation.ValidationException("Change can only be given on cash tenders.");
                var cashTendered = sale.Payments.Where(p => p.Tender == TenderType.Cash).Sum(p => p.Amount);
                if (overpay > cashTendered)
                    throw new FluentValidation.ValidationException("Change cannot exceed the cash portion of the payment.");
                sale.ChangeGiven = overpay;

                // Held sales have no stock effect; completed sales decrement now
                var conflict = false;
                foreach (var line in sale.Lines)
                {
                    try
                    {
                        await stockLedger.AppendAsync([new StockMovementRequest(
                            MovementType.Sale,
                            line.ProductId,
                            sale.LocationId,
                            -line.Qty,
                            VariantId: line.VariantId,
                            BatchId: line.BatchId,
                            CorrelationId: sale.Id,
                            AllowNegative: request.AllowNegativeStock,
                            OccurredAt: sale.OccurredAt)], cancellationToken);
                    }
                    catch (ConflictException) when (request.AllowNegativeStock)
                    {
                        conflict = true;
                    }

                    // Offline ingest drove the projection negative → flag, never hide (FR-046)
                    var level = await context.StockLevels.FirstOrDefaultAsync(
                        l => l.ProductId == line.ProductId && l.VariantId == line.VariantId &&
                             l.LocationId == sale.LocationId && l.BatchId == line.BatchId, cancellationToken);
                    if (level is not null && level.QtyOnHand < 0) conflict = true;
                }
                sale.StockConflictFlag = conflict && request.AllowNegativeStock;
            }

            context.Sales.Add(sale);
            await context.SaveChangesAsync(cancellationToken);

            if (!isHeld)
                await planEnforcer.IncrementUsageAsync(UsageMetric.SalesThisMonth, cancellationToken: cancellationToken);

            return SaleMapping.ToDto(sale);
        }
    }
}
