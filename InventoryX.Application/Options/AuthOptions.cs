namespace InventoryX.Application.Options;

public class AuthOptions
{
    //Url to append code and userId to when verifying user email
    public string? EmailVerificationUrl { get; set; }
    //Url to append code and email when resetting user password
    public string? ResetPasswordUrl { get; set; }
}
