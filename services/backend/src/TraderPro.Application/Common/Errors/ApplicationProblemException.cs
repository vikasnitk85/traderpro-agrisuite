namespace TraderPro.Application.Common.Errors;

public enum ApplicationErrorCategory
{
    Validation,
    NotFound,
    Conflict,
    Unavailable,
}

public sealed record ApplicationFieldError(
    string Field,
    string Code,
    string Message);

public sealed class ApplicationProblemException : Exception
{
    public ApplicationProblemException(
        string code,
        string message,
        ApplicationErrorCategory category,
        bool retryable = false,
        IReadOnlyList<ApplicationFieldError>? fieldErrors = null,
        IReadOnlyDictionary<string, object?>? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Category = category;
        Retryable = retryable;
        FieldErrors = fieldErrors ?? [];
        Details = details ?? new Dictionary<string, object?>();
    }

    public string Code { get; }

    public ApplicationErrorCategory Category { get; }

    public bool Retryable { get; }

    public IReadOnlyList<ApplicationFieldError> FieldErrors { get; }

    public IReadOnlyDictionary<string, object?> Details { get; }
}
