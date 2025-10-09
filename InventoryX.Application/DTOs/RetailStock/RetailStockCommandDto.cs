using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Application.DTOs.RetailStock
{
    public class RetailStockCommandDto
    {
        public required int InventoryItemId { get; set; }
        public required decimal Quantity { get; set; }
    }
}
