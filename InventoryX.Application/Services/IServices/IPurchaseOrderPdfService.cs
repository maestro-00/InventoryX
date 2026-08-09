namespace InventoryX.Application.Services.IServices;

public sealed record PurchaseOrderDocument(string FileName, string ContentType, byte[] Content);
public sealed record PurchaseOrderEmailResult(Guid PurchaseOrderId, string EmailedTo, string FileName);

public interface IPurchaseOrderPdfService
{
    Task<PurchaseOrderDocument> GenerateAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderEmailResult> EmailAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
}

public interface IAttachmentEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string fileName, string contentType, byte[] content,
        CancellationToken cancellationToken = default);
}
