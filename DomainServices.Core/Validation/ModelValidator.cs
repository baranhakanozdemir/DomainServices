namespace DomainServices.Core.Validation;

public sealed class ModelValidator
{
    private readonly List<ValidationResult> _results = new();

    public IReadOnlyList<ValidationResult> Results => _results;

    public IEnumerable<ValidationResult> Errors =>
        _results.Where(r => r.Severity == ValidationSeverity.Error);

    public IEnumerable<ValidationResult> Warnings =>
        _results.Where(r => r.Severity == ValidationSeverity.Warning);

    public bool IsValid => _results.All(r => r.Severity != ValidationSeverity.Error);

    public string ErrorMessages =>
        string.Join(Environment.NewLine, Errors.Select(e => e.ToString()));

    public ModelValidator AddError(string property, string message)
    {
        _results.Add(new ValidationResult(property, message, ValidationSeverity.Error));
        return this;
    }

    public ModelValidator AddWarning(string property, string message)
    {
        _results.Add(new ValidationResult(property, message, ValidationSeverity.Warning));
        return this;
    }

    public ModelValidator AddInfo(string property, string message)
    {
        _results.Add(new ValidationResult(property, message, ValidationSeverity.Info));
        return this;
    }

    public ModelValidator Require(bool condition, string property, string message)
    {
        if (!condition)
        {
            AddError(property, message);
        }
        return this;
    }

    public ModelValidator RequireNotNull(object? value, string property, string? message = null) =>
        Require(value is not null, property, message ?? $"{property} is required.");

    public ModelValidator RequireNotEmpty(string? value, string property, string? message = null) =>
        Require(!string.IsNullOrWhiteSpace(value), property, message ?? $"{property} is required.");

    public ModelValidator RequireNotEmpty(Guid value, string property, string? message = null) =>
        Require(value != Guid.Empty, property, message ?? $"{property} is required.");

    public ModelValidator Merge(ModelValidator other)
    {
        _results.AddRange(other._results);
        return this;
    }
}
