using InventoryX.Application.Exceptions;
using InventoryX.Application.Queries.Requests.Selling;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Selling;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace InventoryX.Application.Queries.RequestHandlers.Selling;
public sealed class GetZReportQueryHandler(IAppDbContext context, IPosAccess posAccess) : IRequestHandler<GetZReportQuery, ZReportDto>
{
 public async Task<ZReportDto> Handle(GetZReportQuery request, CancellationToken ct)
 {
     var shift = await context.Shifts.SingleOrDefaultAsync(x => x.Id == request.ShiftId, ct) ?? throw new NotFoundException("Shift not found.");
     await posAccess.EnsureCanViewShiftAsync(shift, ct);
     var sales = await context.Sales.Where(x => x.ShiftId == shift.Id).ToListAsync(ct);
     var ids = sales.Select(x => x.Id);
     var cash = await context.SalePayments.Where(x => ids.Contains(x.SaleId) && x.Tender == TenderType.Cash).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
     var movements = await context.CashMovements.Where(x => x.ShiftId == shift.Id).ToListAsync(ct);
     var expectedCash = shift.ExpectedCash ?? shift.OpeningFloat + cash - sales.Sum(x => x.ChangeGiven)
         + movements.Sum(x => x.Type == CashMovementType.CashIn ? x.Amount : -x.Amount);
     return new(shift.Id, shift.RegisterId, shift.OpenedBy, sales.Where(x => x.Status == SaleStatus.Completed).Sum(x => x.GrandTotal), cash, sales.Where(x => x.Status is SaleStatus.Returned or SaleStatus.PartiallyReturned).Sum(x => x.GrandTotal), sales.Sum(x => x.DiscountTotal), sales.Count(x => x.Status == SaleStatus.Voided), expectedCash, shift.ClosingCounted, shift.Variance);
 }
}
