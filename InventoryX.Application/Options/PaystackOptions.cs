namespace InventoryX.Application.Options;

public sealed class PaystackOptions
{
    public const string SectionName = "Paystack";
    public string BaseUrl { get; set; } = "https://api.paystack.co/";
    public string SecretKey { get; set; } = string.Empty;
    public string? CallbackUrl { get; set; }
}
