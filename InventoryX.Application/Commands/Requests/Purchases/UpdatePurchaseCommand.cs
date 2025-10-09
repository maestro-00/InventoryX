using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.DTOs.Purchases;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Purchases
{
    public class UpdatePurchaseCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
        public required PurchaseCommandDto PurchaseDto { get; set; }
    }
}
