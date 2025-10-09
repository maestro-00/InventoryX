using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models
{
    public class RetailStock : BaseModel
    {
        [ForeignKey("InventoryItemId")]
        public required int InventoryItemId { get; set; }
        public InventoryItem InventoryItem { get; set; }
        [Column(TypeName = "decimal(17,3)")]
        public required decimal Quantity { get; set; }
    }
}
