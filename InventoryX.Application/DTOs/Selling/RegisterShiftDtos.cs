namespace InventoryX.Application.DTOs.Selling
{
    public class RegisterDto
    {
        public Guid Id { get; init; }
        public Guid LocationId { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public byte[]? RowVersion { get; init; }
    }

    public class ShiftDto
    {
        public Guid Id { get; init; }
        public Guid RegisterId { get; init; }
        public string OpenedBy { get; init; } = string.Empty;
        public DateTime OpenedAt { get; init; }
        public decimal OpeningFloat { get; init; }
        public string Status { get; init; } = "Open";
    }

    public sealed class CashMovementDto
    {
        public Guid Id { get; init; }
        public decimal Amount { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }
}
