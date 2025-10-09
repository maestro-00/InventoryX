using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using InventoryX.Application.Commands.Requests.Sales;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using MediatR;

namespace InventoryX.Application.Commands.RequestHandlers.Sales
{
    public class UpdateSaleCommandHandler(ISaleService service, IMapper mapper) : IRequestHandler<UpdateSaleCommand, ApiResponse>
    {
        private readonly ISaleService _service = service;
        private readonly IMapper _mapper = mapper;

        public async Task<ApiResponse> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var SaleEntity = _mapper.Map<Sale>(request.SaleDto);
                SaleEntity.Id = request.Id;
                SaleEntity.Updated_At = DateTime.UtcNow;
                var response = await _service.UpdateSale(SaleEntity);
                if (response > 0)
                {
                    return new()
                    {
                        Id = SaleEntity.Id,
                        Success = true,
                        Message = "Sale has been updated successfully"
                    };
                }
                throw new Exception("Failed to update Sale");
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
