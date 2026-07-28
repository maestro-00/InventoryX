namespace InventoryX.Application.DTOs.Selling;

public record ReceiptDto(Guid Id, Guid SaleId, string Number, string PayloadJson, DateTime CreatedAt);

public record ReceiptTemplateDto(string TemplateJson);
