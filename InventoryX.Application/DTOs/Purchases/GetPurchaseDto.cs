using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.InventoryItems;
using InventoryX.Application.DTOs.Users;

namespace InventoryX.Application.DTOs.Purchases
{
    public class GetPurchaseDto : BaseDto
    {
        public GetInventoryItemDto InventoryItem { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public GetUserDto Purchaser { get; set; }
    }
}
