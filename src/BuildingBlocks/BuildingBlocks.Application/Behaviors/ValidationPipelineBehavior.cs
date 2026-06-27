using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Messaging;
using FluentValidation;
using ValidationException = BuildingBlocks.Application.Exceptions.ValidationException;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Runs all FluentValidation validators registered for the request before it reaches
/// the handler. On failure throws <see cref="ValidationException"/> (converted to
/// ProblemDetails at the API boundary) so handlers never see invalid input.
/// </summary>
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => new ValidationError(failure.PropertyName, failure.ErrorMessage))
            .ToArray();

        if (failures.Length != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
