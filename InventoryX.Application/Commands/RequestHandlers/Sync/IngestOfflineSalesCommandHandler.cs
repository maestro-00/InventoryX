using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Commands.Requests.Sync;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Domain.Models.Sync;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Sync;

public sealed class IngestOfflineSalesCommandHandler(
    IAppDbContext context,
    IStockLedger stockLedger,
    ITaxCalculator taxCalculator,
    ITenantContext tenantContext,
    IPlanEnforcer planEnforcer,
    IReceiptBuilder? receiptBuilder = null,
    INotificationService? notificationService = null,
    IHttpContextAccessor? httpContextAccessor = null) : IRequestHandler<IngestOfflineSalesCommand, List<OfflineSaleIngestResult>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<OfflineSaleIngestResult>> Handle(IngestOfflineSalesCommand request, CancellationToken cancellationToken)
    {
        if (request.Sales.Count == 0)
            throw new FluentValidation.ValidationException("At least one offline sale is required.");

        if (httpContextAccessor?.HttpContext?.Items.TryGetValue("RegisterToken.ClaimedRegisterId", out var claimed) == true
            && claimed is Guid claimedRegisterId)
        {
            if (request.Sales.Any(sale => sale.RegisterId != claimedRegisterId))
                throw new CustomException("Register token may only ingest sales for its own register.", 403);
        }

        var processor = new CreateSaleCommandHandler(context, stockLedger, taxCalculator, tenantContext, planEnforcer, receiptBuilder);
        var results = new List<OfflineSaleIngestResult>(request.Sales.Count);
        foreach (var input in request.Sales)
        {
            try
            {
                var released = await context.RejectedOfflineSales
                    .Where(r => r.ClientSaleId == input.ClientSaleId &&
                                r.Status == RejectedOfflineSaleStatus.ReleasedForRetry)
                    .ToListAsync(cancellationToken);
                if (released.Count > 0)
                {
                    context.RejectedOfflineSales.RemoveRange(released);
                    await context.SaveChangesAsync(cancellationToken);
                }

                var sale = await processor.Handle(ToOfflineCommand(input), cancellationToken);
                results.Add(new OfflineSaleIngestResult(
                    input.ClientSaleId,
                    sale.Id,
                    sale.StockConflictFlag ? "applied_with_conflict" : "applied"));
                if (sale.StockConflictFlag && notificationService is not null)
                    await notificationService.RaiseAsync(
                        NotificationType.StockConflict,
                        ResolveSyncConflictCommandHandler.Key(sale.Id),
                        "Offline stock conflict",
                        $"Sale {sale.Id} caused or encountered contested stock and requires review.",
                        cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is FluentValidation.ValidationException
                                               or NotFoundException
                                               or ConflictException)
            {
                var payloadJson = JsonSerializer.Serialize(input, SerializerOptions);
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));
                var existingOpen = await context.RejectedOfflineSales
                    .FirstOrDefaultAsync(r => r.ClientSaleId == input.ClientSaleId &&
                                              r.Status == RejectedOfflineSaleStatus.Open, cancellationToken);
                if (existingOpen is null)
                {
                    context.RejectedOfflineSales.Add(new RejectedOfflineSale
                    {
                        ClientSaleId = input.ClientSaleId,
                        RegisterId = input.RegisterId,
                        ShiftId = input.ShiftId,
                        PayloadJson = payloadJson,
                        PayloadHash = hash,
                        RejectionReason = exception.Message,
                        TraceId = System.Diagnostics.Activity.Current?.Id,
                    });
                    await context.SaveChangesAsync(cancellationToken);
                }

                results.Add(new OfflineSaleIngestResult(input.ClientSaleId, null, "rejected", exception.Message));
            }
        }
        return results;
    }

    private static CreateSaleCommand ToOfflineCommand(CreateSaleCommand input) => new()
    {
        ClientSaleId = input.ClientSaleId,
        RegisterId = input.RegisterId,
        ShiftId = input.ShiftId,
        Status = input.Status,
        Lines = input.Lines,
        Payments = input.Payments,
        OccurredAt = input.OccurredAt,
        OfflineOrigin = true,
        AllowNegativeStock = true,
        AcceptHistoricalFiscalSnapshot = true,
    };
}
