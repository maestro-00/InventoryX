using FluentValidation;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Selling;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Selling;
public sealed class RecordCashMovementCommandHandler(IAppDbContext context, ITenantContext tenantContext) : IRequestHandler<RecordCashMovementCommand, CashMovementDto>
{
    public async Task<CashMovementDto> Handle(RecordCashMovementCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0 || !Enum.TryParse<CashMovementType>(request.Type, true, out var type)) throw new ValidationException("A positive amount and CashIn or CashOut type are required.");
        if (!await context.Shifts.AnyAsync(item => item.Id == request.ShiftId && item.Status == ShiftStatus.Open, cancellationToken)) throw new ConflictException("Cash movement requires an open shift.");
        var movement = new CashMovement { ShiftId = request.ShiftId, Type = type, Amount = request.Amount, Reason = request.Reason, RecordedBy = tenantContext.UserId ?? "unknown", RecordedAt = DateTime.UtcNow };
        context.CashMovements.Add(movement); await context.SaveChangesAsync(cancellationToken);
        return new CashMovementDto { Id = movement.Id, Amount = movement.Amount, Type = movement.Type.ToString(), Reason = movement.Reason };
    }
}
public sealed class CloseShiftCommandHandler(IAppDbContext context, ITenantContext tenantContext) : IRequestHandler<CloseShiftCommand, ShiftDto>
{
    public async Task<ShiftDto> Handle(CloseShiftCommand request, CancellationToken cancellationToken)
    {
        if (request.ClosingCounted is null) throw new ValidationException("A counted closing drawer is required.");
        var shift = await context.Shifts.SingleOrDefaultAsync(item => item.Id == request.ShiftId && item.Status == ShiftStatus.Open, cancellationToken) ?? throw new NotFoundException("Open shift not found.");
        var saleIds = context.Sales.Where(item => item.ShiftId == shift.Id).Select(item => item.Id);
        var payments = await context.SalePayments.Where(item => saleIds.Contains(item.SaleId) && item.Tender == TenderType.Cash).SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;
        var change = await context.Sales.Where(item => item.ShiftId == shift.Id).SumAsync(item => (decimal?)item.ChangeGiven, cancellationToken) ?? 0m;
        var movements = await context.CashMovements.Where(item => item.ShiftId == shift.Id).ToListAsync(cancellationToken);
        shift.ExpectedCash = shift.OpeningFloat + payments - change + movements.Sum(item => item.Type == CashMovementType.CashIn ? item.Amount : -item.Amount);
        shift.ClosingCounted = request.ClosingCounted; shift.Variance = request.ClosingCounted - shift.ExpectedCash; shift.Status = ShiftStatus.Closed; shift.ClosedAt = DateTime.UtcNow; shift.ClosedBy = tenantContext.UserId ?? "unknown";
        await context.SaveChangesAsync(cancellationToken);
        return new ShiftDto { Id = shift.Id, RegisterId = shift.RegisterId, OpenedBy = shift.OpenedBy, OpenedAt = shift.OpenedAt, OpeningFloat = shift.OpeningFloat, Status = shift.Status.ToString() };
    }
}
