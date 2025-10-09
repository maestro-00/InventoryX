using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models
{
    public class InventoryItemType : BaseModel
    {
        public required string Name { get; set; }
    }
}
