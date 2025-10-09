using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.Common;
using InventoryX.Domain.Models;

namespace InventoryX.Application.DTOs.InventoryItemTypes
{
    public class GetInventoryItemTypeDto : BaseDto
    {
        public string Name { get; set; }
    }
}
