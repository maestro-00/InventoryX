using InventoryX.Application.Behaviors;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Import
{
    public record ImportJobDto(
        Guid Id,
        string Kind,
        string FileName,
        string Status,
        List<string> DetectedColumns,
        List<ImportRowResult>? Preview,
        int CreatedCount,
        int UpdatedCount,
        int SkippedCount);

    /// <summary>Step 1 — upload; detects columns, saves the job, persists nothing to the catalogue.</summary>
    public class CreateImportJobCommand : IRequest<ImportJobDto>, ITenantWriteCommand
    {
        /// <summary>Products | OpeningStock.</summary>
        public required string Kind { get; init; }
        public required string FileName { get; init; }
        public required byte[] FileContent { get; init; }
    }

    /// <summary>Step 2 — set the column mapping; returns the full per-row preview.</summary>
    public class SetImportMappingCommand : IRequest<ImportJobDto>, ITenantWriteCommand
    {
        public Guid JobId { get; init; }
        public Dictionary<string, string> ColumnMapping { get; init; } = [];
    }

    /// <summary>Step 3 — persist valid rows; per-row errors never abort the batch.</summary>
    public class CommitImportCommand : IRequest<ImportJobDto>, ITenantWriteCommand
    {
        public Guid JobId { get; init; }
        /// <summary>Target location for opening-stock imports.</summary>
        public Guid? LocationId { get; init; }
    }

    public class AbandonImportCommand : IRequest<bool>, ITenantWriteCommand
    {
        public Guid JobId { get; init; }
    }
}
