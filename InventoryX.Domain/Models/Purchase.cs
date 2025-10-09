using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models
{
    public class Purchase : BaseModel
    {
        [ForeignKey("InventoryItemId")]
        public int InventoryItemId { get; set; }
        public required InventoryItem InventoryItem { get; set; }
        [Column(TypeName = "decimal(17,3)")]
        public decimal Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public required string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual required User Purchaser { get; set; }
    }
}
