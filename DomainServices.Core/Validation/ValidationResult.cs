namespace DomainServices.Core.Validation;

public sealed class ValidationResult
{
    public ValidationResult(string property, string message, ValidationSeverity severity = ValidationSeverity.Error)
    {
        Property = property;
        Message = message;
        Severity = severity;
    }

    public string Property { get; }

    public string Message { get; }

    public ValidationSeverity Severity { get; }

    public override string ToString() => $"[{Severity}] {Property}: {Message}";
}

public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}
