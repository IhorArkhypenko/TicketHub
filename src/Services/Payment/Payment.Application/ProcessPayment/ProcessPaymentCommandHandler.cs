using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Contracts.Events.Payment.V1;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;
using Payment.Domain;

namespace Payment.Application.ProcessPayment;

/// <summary>
/// Idempotent charge handler. The booking id is the idempotency key: if a payment record already
/// exists for the booking, the prior outcome is re-published instead of charging again. Combined
/// with the message Inbox (dedup by messageId), at-least-once delivery never debits twice.
/// </summary>
internal sealed class ProcessPaymentCommandHandler : ICommandHandler<ProcessPaymentCommand>
{
    private readonly IPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IPaymentSimulator _simulator;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository payments,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IPaymentSimulator simulator,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _simulator = simulator;
        _logger = logger;
    }

    public async Task<Result> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        PaymentRecord? existing = await _payments.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Payment for booking {BookingId} already processed with status {Status}; re-publishing outcome",
                request.BookingId, existing.Status);

            await PublishOutcomeAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var record = PaymentRecord.Start(request.BookingId, request.Amount, request.Currency);
        PaymentOutcome outcome = _simulator.Charge(request.Amount, request.Currency);

        if (outcome.Succeeded)
        {
            record.MarkCompleted();
        }
        else
        {
            record.MarkFailed(outcome.FailureReason ?? "Payment declined.");
        }

        await _payments.AddAsync(record, cancellationToken);
        await PublishOutcomeAsync(record, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Task PublishOutcomeAsync(PaymentRecord record, CancellationToken cancellationToken)
        => record.Status == PaymentStatus.Completed
            ? _publisher.PublishAsync(
                new PaymentCompleted(record.BookingId, record.Id, record.Amount, record.Currency, DateTime.UtcNow),
                cancellationToken)
            : _publisher.PublishAsync(
                new PaymentFailed(record.BookingId, record.FailureReason ?? "Payment declined.", DateTime.UtcNow),
                cancellationToken);
}
