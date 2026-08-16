using System.Security.Claims;
using InventoryX.Application.Options;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryX.Presentation.Authentication;

public static class GoogleOAuthHandler
{
    public static async Task OnTicketReceived(TicketReceivedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();
        var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var jwtOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<JwtOptions>>();
        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("InventoryX.Presentation.Authentication.GoogleOAuthHandler");

        var returnUrl = "/";
        if (context.Properties?.Items != null &&
            context.Properties.Items.TryGetValue("returnUrl", out var url) &&
            !string.IsNullOrWhiteSpace(url))
        {
            returnUrl = url;
        }
        else if (!string.IsNullOrWhiteSpace(context.Properties?.RedirectUri))
        {
            returnUrl = context.Properties.RedirectUri;
        }

        var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
        var nameIdentifier = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogInformation("OAuth callback received for email: {Email}", email);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(nameIdentifier))
        {
            logger.LogWarning("OAuth callback missing email or nameIdentifier");
            context.ReturnUri = returnUrl;
            return;
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            logger.LogInformation("Creating new user for email: {Email}", email);

            user = new User();
            await userManager.SetUserNameAsync(user, email);
            await userManager.SetEmailAsync(user, email);

            var name = context.Principal?.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrEmpty(name))
                user.Name = name;

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                context.ReturnUri = returnUrl;
                return;
            }

            var confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await userManager.ConfirmEmailAsync(user, confirmToken);

            var loginInfo = new UserLoginInfo(context.Scheme.Name, nameIdentifier, context.Scheme.DisplayName);
            await userManager.AddLoginAsync(user, loginInfo);

            logger.LogInformation("User created successfully: {Email}", email);
        }
        else
        {
            logger.LogInformation("User already exists: {Email}", email);

            var existingLogins = await userManager.GetLoginsAsync(user);
            if (!existingLogins.Any(l => l.LoginProvider == context.Scheme.Name && l.ProviderKey == nameIdentifier))
            {
                var loginInfo = new UserLoginInfo(context.Scheme.Name, nameIdentifier, context.Scheme.DisplayName);
                await userManager.AddLoginAsync(user, loginInfo);
                logger.LogInformation("External login linked to existing user: {Email}", email);
            }
        }

        await signInManager.SignInAsync(user, isPersistent: true, authenticationMethod: IdentityConstants.ApplicationScheme);

        var role = user.RoleId is null
            ? null
            : await db.AppRoles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == user.RoleId);
        var tokens = tokenService.CreateTokenPair(user, role);
        AuthSessionCookies.Set(context.HttpContext.Response, tokens.RefreshToken, jwtOptions);

        var redirectParams = new Dictionary<string, string?>
        {
            ["accessToken"] = tokens.AccessToken,
            ["refreshToken"] = tokens.RefreshToken,
            ["accessTokenExpiresAt"] = tokens.AccessTokenExpiresAt.ToUniversalTime().ToString("O"),
        };
        context.ReturnUri = QueryHelpers.AddQueryString(returnUrl, redirectParams);

        logger.LogInformation("User signed in successfully: {Email}", email);
    }
}
