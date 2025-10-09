using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Domain.Models.Common
{
    public class BaseModel
    {
        public int Id { get; set; }
        public DateTime? Created_At { get; set; }
        public DateTime? Updated_At { get; set; }
    }
}
