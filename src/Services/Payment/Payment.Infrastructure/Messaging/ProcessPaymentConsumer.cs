using BuildingBlocks.Application.Messaging;
using Contracts.Events.Payment.V1;
using MassTransit;
using Payment.Application.Abstractions;
using Payment.Application.ProcessPayment;

namespace Payment.Infrastructure.Messaging;

/// <summary>
/// Consumes the ProcessPayment Saga command and drives the idempotent charge handler. The
/// receive endpoint is protected by the EF Inbox (dedup by messageId); business-key idempotency
/// is enforced in the handler.
/// </summary>
public sealed class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    private readonly ISender _sender;

    public ProcessPaymentConsumer(ISender sender) => _sender = sender;

    public Task Consume(ConsumeContext<ProcessPayment> context)
        => _sender.Send(
            new ProcessPaymentCommand(context.Message.BookingId, context.Message.Amount, context.Message.Currency),
            context.CancellationToken);
}

internal sealed class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventPublisher(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
        => _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
