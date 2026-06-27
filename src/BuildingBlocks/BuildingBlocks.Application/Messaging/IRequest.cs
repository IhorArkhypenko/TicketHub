using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Messaging;

/// <summary>Marker for a request (command or query) that yields a <typeparamref name="TResponse"/>.</summary>
public interface IRequest<TResponse>;

/// <summary>A command that mutates state and returns a <see cref="Result"/>.</summary>
public interface ICommand : IRequest<Result>;

/// <summary>A command that mutates state and returns a value.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>A read-only query returning a value.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
