using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace InventoryX.Application.Services
{
    public class AuthService(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager) : IAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly UserManager<User> _userManager = userManager;

        public async Task<User> GetAuthenticatedUser()
        {
            var httpCAccessor = _httpContextAccessor.HttpContext.User;
            var user = await _userManager.GetUserAsync(httpCAccessor);

            return user ?? throw new InvalidOperationException("Invalid User");
        }

    }
}
