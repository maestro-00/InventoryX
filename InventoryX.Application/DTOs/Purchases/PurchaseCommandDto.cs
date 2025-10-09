using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models;

namespace InventoryX.Application.DTOs.Purchases
{
    public class PurchaseCommandDto
    {
        [Required]
        public required int InventoryItemId { get; set; }
        [Required]
        public decimal Quantity { get; set; }
        [Required]
        public decimal Price { get; set; }
    }
}
