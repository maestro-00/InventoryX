using FluentValidation;
using MediatR;

namespace InventoryX.Application.Behaviors
{
    /// <summary>
    /// Runs every registered FluentValidation validator for the request before
    /// the handler executes (constitution Principle IV). Failures throw
    /// ValidationException → 400 problem details.
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
                if (failures.Count > 0) throw new ValidationException(failures);
            }

            return await next(cancellationToken);
        }
    }
}
