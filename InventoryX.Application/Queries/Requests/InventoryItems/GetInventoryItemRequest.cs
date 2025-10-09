using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs;
using MediatR;

namespace InventoryX.Application.Queries.Requests.InventoryItems
{
    public class GetInventoryItemRequest : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }
}
