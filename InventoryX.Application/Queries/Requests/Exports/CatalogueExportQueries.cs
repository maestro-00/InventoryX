using InventoryX.Application.Behaviors;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Exports;

public sealed record ExportCatalogueQuery(string Resource, string Format, bool IncludeCost)
    : IRequest<ReportExportDocument>, IReadOnlyWriteExemptCommand;
