using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryX.Application.DTOs.Common
{
    public class BaseDto
    {
        public int Id { get; set; }
        public DateTime? Created_At { get; set; }
    }
}
