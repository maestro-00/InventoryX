using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using InventoryX.Application.DTOs.Sales;
using InventoryX.Application.Queries.Requests.Sales;
using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Queries.RequestHandlers.Sales
{
    public class GetSaleRequestHandler(ISaleService service, IMapper mapper) : IRequestHandler<GetSaleRequest, ApiResponse>
    {
        private readonly ISaleService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetSaleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetSale(request.Id) ?? throw new Exception("Sale does not exist");
                var SaleDto = _mapper.Map<GetSaleDto>(response);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Retrieved Sale successfully",
                    Body = SaleDto
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message ?? "Something went wrong. Try again later."
                };

            }
        }
    }
}
