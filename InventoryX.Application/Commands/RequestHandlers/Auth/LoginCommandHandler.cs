using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Exceptions;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Application.Commands.RequestHandlers.Auth
{
    public class LoginCommandHandler(
        IAppDbContext context,
        UserManager<User> userManager,
        ITokenService tokenService) : IRequestHandler<LoginCommand, LoginResult>
    {
        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                throw new CustomException("Invalid email or password.", 401);
            if (user.Status == UserStatus.Deactivated)
                throw new CustomException("This account has been deactivated.", 403);

            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(request.TwoFactorCode))
                    return new LoginResult(true, null, null, null);
                var valid = await userManager.VerifyTwoFactorTokenAsync(
                    user, userManager.Options.Tokens.AuthenticatorTokenProvider, request.TwoFactorCode);
                if (!valid) throw new CustomException("Invalid two-factor code.", 401);
            }

            var role = user.RoleId is null
                ? null
                : await context.AppRoles.FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken);
            var tokens = tokenService.CreateTokenPair(user, role);
            return new LoginResult(false, tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.RefreshToken);
        }
    }

    public class RefreshTokenCommandHandler(
        IAppDbContext context,
        UserManager<User> userManager,
        ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, LoginResult>
    {
        public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var userId = tokenService.ValidateRefreshToken(request.RefreshToken)
                ?? throw new CustomException("Invalid or expired refresh token.", 401);
            var user = await userManager.FindByIdAsync(userId)
                ?? throw new CustomException("Invalid refresh token.", 401);

            var role = user.RoleId is null
                ? null
                : await context.AppRoles.FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken);
            var tokens = tokenService.CreateTokenPair(user, role);
            return new LoginResult(false, tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.RefreshToken);
        }
    }

    public class EnrollTwoFactorCommandHandler(
        UserManager<User> userManager,
        IAuthService authService) : IRequestHandler<EnrollTwoFactorCommand, TwoFactorEnrollResult>
    {
        public async Task<TwoFactorEnrollResult> Handle(EnrollTwoFactorCommand request, CancellationToken cancellationToken)
        {
            var user = await authService.GetAuthenticatedUser();
            await userManager.ResetAuthenticatorKeyAsync(user);
            var key = await userManager.GetAuthenticatorKeyAsync(user)
                ?? throw new CustomException("Could not generate an authenticator key.", 500);
            var uri = $"otpauth://totp/InventoryX:{Uri.EscapeDataString(user.Email ?? user.Id)}?secret={key}&issuer=InventoryX";
            return new TwoFactorEnrollResult(key, uri);
        }
    }

    public class VerifyTwoFactorCommandHandler(
        UserManager<User> userManager,
        IAuthService authService) : IRequestHandler<VerifyTwoFactorCommand, bool>
    {
        public async Task<bool> Handle(VerifyTwoFactorCommand request, CancellationToken cancellationToken)
        {
            var user = await authService.GetAuthenticatedUser();
            var valid = await userManager.VerifyTwoFactorTokenAsync(
                user, userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);
            if (!valid) throw new CustomException("Invalid verification code.", 400);
            await userManager.SetTwoFactorEnabledAsync(user, true);
            return true;
        }
    }
}
