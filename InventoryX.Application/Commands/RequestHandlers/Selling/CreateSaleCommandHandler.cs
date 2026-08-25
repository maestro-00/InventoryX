using System.Text.Json;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Application.Validators.Selling;
using InventoryX.Domain.Models.Inventory;
using InventoryX.Domain.Models.Catalog;
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
        IPlanEnforcer planEnforcer,
        IPosAccess posAccess,
        IReceiptBuilder? receiptBuilder = null) : IRequestHandler<CreateSaleCommand, SaleDto>
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
            await posAccess.EnsureCanOperateShiftAsync(shift, cancellationToken);

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
                var useHistoricalFiscal = request.AcceptHistoricalFiscalSnapshot
                    && !string.IsNullOrWhiteSpace(lineRequest.TaxComponentsJson)
                    && lineRequest.UnitPrice is not null;
                if (request.AcceptHistoricalFiscalSnapshot && !useHistoricalFiscal)
                    throw new FluentValidation.ValidationException(
                        "Offline sales require UnitPrice and TaxComponentsJson fiscal evidence per line.");
                var grossLineAmount = lineRequest.Qty * unitPrice;
                var discountAuthorizedBy = DiscountPolicyValidator.ResolveAuthorizer(
                    grossLineAmount,
                    lineRequest.LineDiscount,
                    role,
                    tenantContext.UserId,
                    lineRequest.DiscountAuthorizedBy);
                IReadOnlyList<(Guid? BatchId, decimal Quantity)> allocations;
                if (!isHeld && product.TrackingMode == TrackingMode.Batch)
                {
                    allocations = (await stockLedger.AllocateFefoAsync(product.Id, variant?.Id, register.LocationId,
                        lineRequest.Qty, lineRequest.BatchId, request.AllowNegativeStock, cancellationToken))
                        .Select(allocation => ((Guid?)allocation.BatchId, allocation.Quantity)).ToList();
                }
                else
                {
                    if (product.TrackingMode != TrackingMode.Batch && lineRequest.BatchId is not null)
                        throw new FluentValidation.ValidationException("A batch can only be selected for batch-tracked products.");
                    allocations = [(lineRequest.BatchId, lineRequest.Qty)];
                }

                decimal allocatedTax = 0;
                decimal allocatedDiscount = 0;
                decimal? historicalLineTax = null;
                if (useHistoricalFiscal)
                {
                    try
                    {
                        using var document = JsonDocument.Parse(lineRequest.TaxComponentsJson!);
                        if (document.RootElement.ValueKind != JsonValueKind.Array)
                            throw new FormatException();
                        historicalLineTax = document.RootElement.EnumerateArray()
                            .Sum(element => element.TryGetProperty("amount", out var amount)
                                ? amount.GetDecimal()
                                : element.TryGetProperty("Amount", out var amountPascal)
                                    ? amountPascal.GetDecimal()
                                    : 0m);
                    }
                    catch (Exception)
                    {
                        throw new FluentValidation.ValidationException(
                            "TaxComponentsJson must be a JSON array of tax components with amount.");
                    }
                }

                for (var allocationIndex = 0; allocationIndex < allocations.Count; allocationIndex++)
                {
                    var allocation = allocations[allocationIndex];
                    var ratio = allocation.Quantity / lineRequest.Qty;
                    var allocationDiscount = allocationIndex == allocations.Count - 1
                        ? lineRequest.LineDiscount - allocatedDiscount
                        : Math.Round(lineRequest.LineDiscount * ratio, 4);
                    allocatedDiscount += allocationDiscount;
                    var allocationNet = Math.Round(allocation.Quantity * unitPrice - allocationDiscount, 4);
                    string taxComponentsJson;
                    decimal allocationTax;
                    if (useHistoricalFiscal)
                    {
                        taxComponentsJson = lineRequest.TaxComponentsJson!;
                        allocationTax = allocationIndex == allocations.Count - 1
                            ? historicalLineTax!.Value - allocatedTax
                            : Math.Round(historicalLineTax!.Value * ratio, 2);
                    }
                    else
                    {
                        var components = taxCalculator.Calculate(allocationNet, product.TaxTreatment?.ComponentsJson ?? "[]");
                        taxComponentsJson = JsonSerializer.Serialize(components, SerializerOptions);
                        allocationTax = Math.Round(components.Sum(component => component.Amount), 2);
                    }
                    allocatedTax += allocationTax;
                    sale.Lines.Add(new SaleLine
                    {
                        ProductId = product.Id, VariantId = variant?.Id, BatchId = allocation.BatchId,
                        ProductName = variant is null ? product.Name : $"{product.Name} ({variant.Sku ?? "variant"})",
                        Qty = allocation.Quantity, UnitPrice = unitPrice, PriceOverridden = lineRequest.UnitPrice is not null,
                        LineDiscount = allocationDiscount, DiscountAuthorizedBy = discountAuthorizedBy,
                        TaxComponents = taxComponentsJson, TaxAmount = allocationTax,
                        LineTotal = Math.Round(allocationNet + allocationTax, 2), Note = lineRequest.Note,
                    });
                }

                subtotal += Math.Round(lineRequest.Qty * unitPrice, 2);
                discountTotal += lineRequest.LineDiscount;
                taxTotal += allocatedTax;
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
            if (!isHeld && receiptBuilder is not null)
                await receiptBuilder.BuildAsync(sale, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            if (!isHeld)
                await planEnforcer.IncrementUsageAsync(UsageMetric.SalesThisMonth, cancellationToken: cancellationToken);

            return SaleMapping.ToDto(sale);
        }
    }
}
