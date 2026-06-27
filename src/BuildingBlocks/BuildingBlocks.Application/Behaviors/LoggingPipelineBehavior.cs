using System.Diagnostics;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Structured logging around every request: name, outcome (success/failure with error
/// code) and elapsed time. Correlation/trace ids are attached by the logging provider.
/// </summary>
public sealed class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        long startTimestamp = Stopwatch.GetTimestamp();

        _logger.LogInformation("Handling {RequestName}", requestName);

        TResponse response = await next();

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        if (response is Result { IsFailure: true } result)
        {
            _logger.LogWarning(
                "{RequestName} failed in {ElapsedMs}ms with {ErrorCode}: {ErrorMessage}",
                requestName, elapsed.TotalMilliseconds, result.Error.Code, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("{RequestName} handled in {ElapsedMs}ms", requestName, elapsed.TotalMilliseconds);
        }

        return response;
    }
}
