using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.Common;
using InventoryX.Application.DTOs.InventoryItemTypes;
using InventoryX.Domain.Models;

namespace InventoryX.Application.DTOs.InventoryItems
{
    public class GetInventoryItemDto : BaseDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public GetInventoryItemTypeDto Type { get; set; }
        public byte[]? Image { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAmount { get; set; }
        public string? SKU { get; set; }
        public decimal ReOrderLevel { get; set; }
    }
}
