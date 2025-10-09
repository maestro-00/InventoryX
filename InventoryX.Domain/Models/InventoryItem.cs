using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models
{
    public class InventoryItem : BaseModel
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        [ForeignKey("TypeId")]
        public required int TypeId { get; set; }
        public virtual InventoryItemType Type { get; set; }
        public byte[]? Image { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Price { get; set; }
        [Column(TypeName = "decimal(17,3)")]
        public decimal TotalAmount { get; set; }
    }
}
