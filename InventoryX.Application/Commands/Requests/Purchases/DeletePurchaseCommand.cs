using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Purchases
{
    public class DeletePurchaseCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }
}
