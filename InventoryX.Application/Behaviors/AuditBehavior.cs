using InventoryX.Application.Services.IServices;
using MediatR;

namespace InventoryX.Application.Behaviors
{
    /// <summary>
    /// Writes an append-only AuditLogEntry for every successfully handled
    /// command marked IAuditedCommand (FR-008).
    /// </summary>
    public class AuditBehavior<TRequest, TResponse>(IAuditLogWriter auditLogWriter)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next(cancellationToken);

            if (request is IAuditedCommand audited)
            {
                await auditLogWriter.WriteAsync(
                    audited.AuditAction,
                    audited.AuditEntityType,
                    audited.AuditEntityId,
                    before: null,
                    after: request,
                    cancellationToken);
            }

            return response;
        }
    }
}
