using MediatR;
namespace InventoryX.Application.Queries.Requests.Selling;
public sealed record ZReportDto(Guid ShiftId, Guid RegisterId, string Staff, decimal Sales, decimal CashTendered, decimal Refunds, decimal Discounts, int Voids, decimal ExpectedCash, decimal? CountedCash, decimal? Variance);
public sealed record GetZReportQuery(Guid ShiftId) : IRequest<ZReportDto>;
