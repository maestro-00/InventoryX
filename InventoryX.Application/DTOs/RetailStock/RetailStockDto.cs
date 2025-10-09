using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.Common;

namespace InventoryX.Application.DTOs.RetailStock
{
    public class RetailStockDto : BaseDto
    {
        public required int InventoryItemId { get; set; }
        public required decimal Quantity { get; set; }
    }
}
