namespace InventoryX.Application.DTOs.Users;

public class ExternalLoginRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}
