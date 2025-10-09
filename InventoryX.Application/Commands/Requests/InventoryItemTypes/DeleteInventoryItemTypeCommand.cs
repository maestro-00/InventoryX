using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace InventoryX.Application.Commands.Requests.InventoryItemTypes
{
    public class DeleteInventoryItemTypeCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }
}
