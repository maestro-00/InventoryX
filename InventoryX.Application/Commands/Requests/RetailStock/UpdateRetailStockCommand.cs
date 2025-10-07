using InventoryX.Application.DTOs.RetailStock;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Application.Commands.Requests.RetailStock
{
    public class UpdateRetailStockCommand : IRequest<ApiResponse>
    {
        public required RetailStockCommandDto RetailStock { get; set; }
    }
}
