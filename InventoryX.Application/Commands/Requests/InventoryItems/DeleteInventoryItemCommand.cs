using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace InventoryX.Application.Commands.Requests.InventoryItems
{
    public class DeleteInventoryItemCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }
}
