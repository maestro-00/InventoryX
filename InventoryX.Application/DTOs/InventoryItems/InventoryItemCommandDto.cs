using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models;

namespace InventoryX.Application.DTOs.InventoryItems
{
    public class InventoryItemCommandDto
    {
        [Required]
        public required string Name { get; set; }
        public string? Description { get; set; }
        [Required]
        public required int TypeId { get; set; }
        public byte[]? Image { get; set; }
        public required decimal Price { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
