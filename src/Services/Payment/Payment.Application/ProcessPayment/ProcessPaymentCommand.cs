using BuildingBlocks.Application.Messaging;

namespace Payment.Application.ProcessPayment;

/// <summary>Internal command driven by the ProcessPayment message from the Saga.</summary>
public sealed record ProcessPaymentCommand(Guid BookingId, decimal Amount, string Currency) : ICommand;
