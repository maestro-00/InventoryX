using InventoryX.Application.Commands.Requests.Purchasing; using InventoryX.Application.Repository; using InventoryX.Domain.Models.Purchasing; using MediatR;
namespace InventoryX.Application.Commands.RequestHandlers.Purchasing;
public sealed class CreateSupplierCommandHandler(IAppDbContext context):IRequestHandler<CreateSupplierCommand,SupplierDto>{public async Task<SupplierDto> Handle(CreateSupplierCommand r,CancellationToken ct){var s=new Supplier{Name=r.Name,Email=r.Email,Phone=r.Phone};context.Suppliers.Add(s);await context.SaveChangesAsync(ct);return new(s.Id,s.Name,s.Email,s.Phone);}}
