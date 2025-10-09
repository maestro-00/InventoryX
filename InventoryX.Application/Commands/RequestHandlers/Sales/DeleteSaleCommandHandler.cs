using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using InventoryX.Application.Commands.Requests.Sales;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Commands.RequestHandlers.Sales
{
    public class DeleteSaleCommandHandler(ISaleService service, IMapper mapper) : IRequestHandler<DeleteSaleCommand, ApiResponse>
    {
        private readonly ISaleService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(DeleteSaleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.DeleteSale(request.Id);
                if (response > 0)
                {
                    return new()
                    {
                        Success = true,
                        Message = "Sale has been deleted successfully"
                    };
                }
                throw new Exception("Failed to delete Sale");
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    Message = ex.Message ?? "Something went wrong. Try again later."
                };
            }
        }
    }
}
