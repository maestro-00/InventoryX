using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Inventory;

public record StockCountLineInput(Guid LineId, decimal CountedQty);
public record StockCountLineResult(Guid Id, Guid ProductId, decimal ExpectedQty, decimal? CountedQty, decimal VarianceQty, decimal VarianceValue);
public record StockCountResult(Guid Id, string Scope, string Status, Guid LocationId, List<StockCountLineResult> Lines);

public sealed class OpenStockCountCommand : IRequest<StockCountResult>, ITenantWriteCommand
{
    public Guid LocationId { get; init; }
    public string Scope { get; init; } = "Full";
    public List<Guid> ProductIds { get; init; } = [];
    public Guid? CategoryId { get; init; }
}

public sealed class UpdateStockCountLinesCommand : IRequest<StockCountResult>, ITenantWriteCommand
{
    public Guid CountId { get; init; }
    public List<StockCountLineInput> Lines { get; init; } = [];
}

public abstract class StockCountActionCommand : IRequest<StockCountResult>, ITenantWriteCommand, IAuditedCommand
{
    public Guid CountId { get; init; }
    public abstract string AuditAction { get; }
    public string AuditEntityType => "StockCount";
    public string AuditEntityId => CountId.ToString();
}

public sealed class SubmitStockCountCommand : StockCountActionCommand { public override string AuditAction => "stock.count.submit"; }
public sealed class ApproveStockCountCommand : StockCountActionCommand { public override string AuditAction => "stock.count.approve"; }
public sealed class RejectStockCountCommand : StockCountActionCommand { public override string AuditAction => "stock.count.reject"; }
