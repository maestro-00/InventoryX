using InventoryX.Domain.Models.Common;
namespace InventoryX.Domain.Models.Purchasing;
public sealed class Supplier : BaseModel { public required string Name { get; set; } public string? Email { get; set; } public string? Phone { get; set; } public string? Address { get; set; } public string? Currency { get; set; } public int LeadTimeDays { get; set; } public string? PaymentTerms { get; set; } }
