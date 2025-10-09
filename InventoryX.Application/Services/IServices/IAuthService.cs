using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Services.IServices
{
    public interface IAuthService
    {
        Task<User> GetAuthenticatedUser();
    }
}
