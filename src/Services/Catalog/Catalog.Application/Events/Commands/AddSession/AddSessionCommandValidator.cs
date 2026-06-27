using FluentValidation;

namespace Catalog.Application.Events.Commands.AddSession;

public sealed class AddSessionCommandValidator : AbstractValidator<AddSessionCommand>
{
    public AddSessionCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.StartsAtUtc).GreaterThan(DateTime.UtcNow).WithMessage("Session must start in the future.");
        RuleFor(x => x.Seats).NotEmpty().WithMessage("A session must define at least one seat.");

        RuleForEach(x => x.Seats).ChildRules(seat =>
        {
            seat.RuleFor(s => s.Row).NotEmpty();
            seat.RuleFor(s => s.Number).GreaterThan(0);
            seat.RuleFor(s => s.Price).GreaterThanOrEqualTo(0);
            seat.RuleFor(s => s.Currency).NotEmpty().Length(3);
        });
    }
}
