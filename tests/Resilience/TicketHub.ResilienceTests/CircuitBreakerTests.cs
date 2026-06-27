using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;
using Xunit;

namespace TicketHub.ResilienceTests;

/// <summary>
/// Demonstrates the circuit-breaker behavior used on the gRPC/HTTP dependencies (Polly /
/// Microsoft.Extensions.Resilience): it opens after sustained failures (fast-fail instead of
/// hammering an unavailable dependency) and closes again after the break duration.
/// </summary>
public class CircuitBreakerTests
{
    private static ResiliencePipeline BuildPipeline(TimeSpan breakDuration) =>
        new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = breakDuration,
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>()
            })
            .Build();

    [Fact]
    public async Task Circuit_opens_after_sustained_failures_then_recovers()
    {
        ResiliencePipeline pipeline = BuildPipeline(TimeSpan.FromMilliseconds(500));

        // Drive failures past the minimum throughput so the breaker opens.
        for (int i = 0; i < 2; i++)
        {
            await FluentActions
                .Awaiting(() => pipeline.ExecuteAsync(_ => throw new InvalidOperationException("dependency down")).AsTask())
                .Should().ThrowAsync<InvalidOperationException>();
        }

        // Circuit is now open: calls fast-fail with BrokenCircuitException (no execution).
        await FluentActions
            .Awaiting(() => pipeline.ExecuteAsync(_ => ValueTask.CompletedTask).AsTask())
            .Should().ThrowAsync<BrokenCircuitException>();

        // After the break duration the circuit half-opens; a success closes it.
        await Task.Delay(700);

        bool executed = false;
        await pipeline.ExecuteAsync(_ =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        executed.Should().BeTrue("the circuit should recover and allow calls again");
    }
}
