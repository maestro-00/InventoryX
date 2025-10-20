using System.Security.Claims;
using InventoryX.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace InventoryX.Presentation.Authentication;

public static class GoogleOAuthHandler
{
    public static async Task OnTicketReceived(TicketReceivedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();

        // Get the return URL from authentication properties
        var returnUrl = "/";
        if (context.Properties?.Items != null && context.Properties.Items.TryGetValue("returnUrl", out var url))
        {
            returnUrl = url ?? "/";
        }

        // Get user info from the ticket principal (already authenticated by Google)
        var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
        var nameIdentifier = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(nameIdentifier))
        {
            // Check if user exists
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Create new user
                user = new User();
                await userManager.SetUserNameAsync(user, email);
                await userManager.SetEmailAsync(user, email);

                // Set name if available
                var name = context.Principal?.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(name))
                {
                    user.Name = name;
                }

                var createResult = await userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    // Confirm email for Google users
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    await userManager.ConfirmEmailAsync(user, token);

                    // Add external login
                    var loginInfo = new UserLoginInfo(context.Scheme.Name, nameIdentifier, context.Scheme.DisplayName);
                    await userManager.AddLoginAsync(user, loginInfo);
                }
            }
            else
            {
                // User exists - check if external login is already linked
                var existingLogins = await userManager.GetLoginsAsync(user);
                if (!existingLogins.Any(l => l.LoginProvider == context.Scheme.Name && l.ProviderKey == nameIdentifier))
                {
                    // Link the external login to existing user
                    var loginInfo = new UserLoginInfo(context.Scheme.Name, nameIdentifier, context.Scheme.DisplayName);
                    await userManager.AddLoginAsync(user, loginInfo);
                }
            }

            // Sign in the user with cookie authentication
            await signInManager.SignInAsync(user, isPersistent: false);
        }

        // Redirect to frontend
        context.ReturnUri = returnUrl;
    }
}
