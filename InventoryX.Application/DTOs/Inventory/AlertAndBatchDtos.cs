namespace InventoryX.Application.DTOs.Inventory;

public sealed record ProductBatchRemainingDto(
    Guid Id,
    string BatchNumber,
    DateTime? ExpiresAt,
    DateTime ReceivedAt,
    decimal UnitCost,
    Guid LocationId,
    decimal RemainingQty);

public sealed record AlertDto(
    Guid Id,
    string Type,
    string Channel,
    string Title,
    string? Message,
    DateTime LastRaisedAt,
    int OccurrenceCount);
