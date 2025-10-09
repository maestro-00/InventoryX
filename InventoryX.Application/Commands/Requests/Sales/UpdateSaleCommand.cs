using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.Sales;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Sales
{
    public class UpdateSaleCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
        public required SaleCommandDto SaleDto { get; set; }
    }
}
