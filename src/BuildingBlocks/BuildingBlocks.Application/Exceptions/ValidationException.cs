namespace BuildingBlocks.Application.Exceptions;

public sealed record ValidationError(string PropertyName, string ErrorMessage);

/// <summary>
/// Thrown by the validation pipeline behavior when a request fails FluentValidation.
/// Translated to an RFC 7807 ValidationProblemDetails (HTTP 400) at the API boundary.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyCollection<ValidationError> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyCollection<ValidationError> Errors { get; }
}
