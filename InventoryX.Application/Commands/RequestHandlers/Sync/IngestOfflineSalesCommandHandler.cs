using InventoryX.Application.Commands.RequestHandlers.Selling;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.Commands.Requests.Sync;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Commands.RequestHandlers.Sync;

public sealed class IngestOfflineSalesCommandHandler(
    IAppDbContext context,
    IStockLedger stockLedger,
    ITaxCalculator taxCalculator,
    ITenantContext tenantContext,
    IPlanEnforcer planEnforcer,
    IReceiptBuilder? receiptBuilder = null) : IRequestHandler<IngestOfflineSalesCommand, List<OfflineSaleIngestResult>>
{
    public async Task<List<OfflineSaleIngestResult>> Handle(IngestOfflineSalesCommand request, CancellationToken cancellationToken)
    {
        if (request.Sales.Count == 0)
            throw new FluentValidation.ValidationException("At least one offline sale is required.");
        var processor = new CreateSaleCommandHandler(context, stockLedger, taxCalculator, tenantContext, planEnforcer, receiptBuilder);
        var results = new List<OfflineSaleIngestResult>(request.Sales.Count);
        foreach (var input in request.Sales)
        {
            try
            {
                var sale = await processor.Handle(ToOfflineCommand(input), cancellationToken);
                results.Add(new OfflineSaleIngestResult(
                    input.ClientSaleId,
                    sale.Id,
                    sale.StockConflictFlag ? "applied_with_conflict" : "applied"));
            }
            catch (Exception exception) when (exception is FluentValidation.ValidationException
                                               or Exceptions.NotFoundException
                                               or Exceptions.ConflictException)
            {
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
    };
}
