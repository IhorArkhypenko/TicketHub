using Catalog.Domain.Events;
using FluentValidation;

namespace Catalog.Application.Events.Commands.CreateEvent;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category)
            .Must(category => Enum.TryParse<EventCategory>(category, ignoreCase: true, out _))
            .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<EventCategory>())}.");
    }
}
