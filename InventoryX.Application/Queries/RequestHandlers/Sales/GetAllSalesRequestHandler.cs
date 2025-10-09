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
    public class GetAllSaleRequestHandler(ISaleService service, IMapper mapper) : IRequestHandler<GetAllSaleRequest, ApiResponse>
    {
        private readonly ISaleService _service = service;
        private readonly IMapper _mapper = mapper;
        public async Task<ApiResponse> Handle(GetAllSaleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetAllSales() ?? throw new Exception("Failed to retrieve all sales");
                var SaleDtos = _mapper.Map<IEnumerable<GetSaleDto>>(response);
                return new()
                {
                    Success = true,
                    Message = "Retrieved all sales successfully",
                    Body = SaleDtos
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    Message = ex.Message ?? "Something went wrong. Try again later.",
                };
            }
        }
    }
}
