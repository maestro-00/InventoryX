using InventoryX.Application.Commands.Requests.Purchasing;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Purchasing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Purchasing;

public sealed class PurchaseOrderCommandHandler(IAppDbContext context)
    : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>, IRequestHandler<UpdatePurchaseOrderCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        ValidateLines(request.Lines);
        if (!await context.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException("Supplier not found.");
        var order = new PurchaseOrder
        {
            SupplierId = request.SupplierId, Origin = request.Origin, OriginReferenceId = request.OriginReferenceId,
            RequiredBy = request.RequiredBy, Notes = request.Notes, Lines = MapLines(request.Lines),
        };
        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken);
        return Map(order);
    }

    public async Task<PurchaseOrderDto> Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        ValidateLines(request.Lines);
        var order = await LoadAsync(context, request.Id, cancellationToken);
        if (order.Status != PurchaseOrderStatus.Draft) throw new ConflictException("Only draft purchase orders can be edited.");
        context.PurchaseOrderLines.RemoveRange(order.Lines);
        order.Lines = MapLines(request.Lines);
        order.RequiredBy = request.RequiredBy;
        order.Notes = request.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return Map(order);
    }

    internal static async Task<PurchaseOrder> LoadAsync(IAppDbContext context, Guid id, CancellationToken cancellationToken) =>
        await context.PurchaseOrders.Include(order => order.Lines).SingleOrDefaultAsync(order => order.Id == id, cancellationToken)
        ?? throw new NotFoundException("Purchase order not found.");

    internal static PurchaseOrderDto Map(PurchaseOrder order) => new(order.Id, order.SupplierId, order.Status, order.Origin,
        order.OriginReferenceId, order.RequiredBy, order.Notes, order.Total,
        order.Lines.Select(line => new PurchaseOrderLineDto(line.Id, line.ProductId, line.VariantId, line.Description,
            line.OrderedQty, line.ReceivedQty, line.DamagedQty, line.UnitCost)).ToList());

    private static List<PurchaseOrderLine> MapLines(IEnumerable<PurchaseOrderLineInput> lines) => lines.Select(line => new PurchaseOrderLine
    { ProductId = line.ProductId, VariantId = line.VariantId, Description = line.Description.Trim(), OrderedQty = line.OrderedQty, UnitCost = line.UnitCost }).ToList();

    private static void ValidateLines(IReadOnlyCollection<PurchaseOrderLineInput> lines)
    {
        if (lines.Count == 0 || lines.Any(line => line.ProductId == Guid.Empty || string.IsNullOrWhiteSpace(line.Description) || line.OrderedQty <= 0 || line.UnitCost < 0))
            throw new FluentValidation.ValidationException("At least one valid line with a positive quantity is required.");
    }
}

public sealed class SubmitPurchaseOrderCommandHandler(IAppDbContext context, ITenantContext tenantContext) : IRequestHandler<SubmitPurchaseOrderCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(SubmitPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await PurchaseOrderCommandHandler.LoadAsync(context, request.Id, cancellationToken);
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        var threshold = await context.Tenants.Where(t => t.Id == tenantId).Select(t => t.PoApprovalThreshold).SingleOrDefaultAsync(cancellationToken);
        var requiresApproval = threshold is decimal value && order.Total >= value;
        TryTransition(() => order.Submit(requiresApproval, DateTime.UtcNow));
        await context.SaveChangesAsync(cancellationToken);
        if (requiresApproval) throw new ApprovalRequiredException("Purchase order requires manager approval before it can be sent.", order.Id);
        return PurchaseOrderCommandHandler.Map(order);
    }
    internal static void TryTransition(Action transition) { try { transition(); } catch (InvalidOperationException error) { throw new ConflictException(error.Message); } }
}

public sealed class ApprovePurchaseOrderCommandHandler(IAppDbContext context, ITenantContext tenantContext) : IRequestHandler<ApprovePurchaseOrderCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await PurchaseOrderCommandHandler.LoadAsync(context, request.Id, cancellationToken);
        SubmitPurchaseOrderCommandHandler.TryTransition(() => order.Approve(tenantContext.UserId ?? "unknown", DateTime.UtcNow));
        await context.SaveChangesAsync(cancellationToken); return PurchaseOrderCommandHandler.Map(order);
    }
}

public sealed class RejectPurchaseOrderCommandHandler(IAppDbContext context) : IRequestHandler<RejectPurchaseOrderCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(RejectPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await PurchaseOrderCommandHandler.LoadAsync(context, request.Id, cancellationToken);
        SubmitPurchaseOrderCommandHandler.TryTransition(order.Reject);
        await context.SaveChangesAsync(cancellationToken); return PurchaseOrderCommandHandler.Map(order);
    }
}

public sealed class CancelPurchaseOrderCommandHandler(IAppDbContext context) : IRequestHandler<CancelPurchaseOrderCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await PurchaseOrderCommandHandler.LoadAsync(context, request.Id, cancellationToken);
        SubmitPurchaseOrderCommandHandler.TryTransition(() => order.Cancel(request.Reason, DateTime.UtcNow));
        await context.SaveChangesAsync(cancellationToken); return PurchaseOrderCommandHandler.Map(order);
    }
}

public sealed class SendPurchaseOrderCommandHandler(IPurchaseOrderPdfService documents) : IRequestHandler<SendPurchaseOrderCommand, PurchaseOrderEmailResult>
{
    public Task<PurchaseOrderEmailResult> Handle(SendPurchaseOrderCommand request, CancellationToken cancellationToken) =>
        documents.EmailAsync(request.Id, cancellationToken);
}
