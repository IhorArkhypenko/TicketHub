namespace BuildingBlocks.Application.Messaging;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Cross-cutting behavior wrapped around request handling (validation, logging, etc.).
/// Behaviors form a pipeline executed in registration order, innermost being the handler.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
