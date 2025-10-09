using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Application.DTOs.InventoryItems;
using MediatR;

namespace InventoryX.Application.Commands.Requests.InventoryItems
{
    public class UpdateInventoryItemCommand : IRequest<ApiResponse>
    {
        public int Id { get; set; }
        public required InventoryItemCommandDto InventoryItemDto { get; set; }

        public bool RecordLoss { get; set; }
    }
}
