using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Application.DTOs.InventoryItemTypes
{
    public class InventoryItemTypeCommandDto
    {
        [Required]
        public required string Name { get; set; }
    }
}
