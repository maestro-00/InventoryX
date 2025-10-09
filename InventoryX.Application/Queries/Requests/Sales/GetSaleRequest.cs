using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace InventoryX.Application.Queries.Requests.Sales
{
    public class GetSaleRequest : IRequest<ApiResponse>
    {
        public int Id { get; set; }
    }
}
