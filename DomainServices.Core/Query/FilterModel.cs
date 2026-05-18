using System.Globalization;

namespace DomainServices.Core.Query;

public sealed class FilterModel
{
    public FilterModel(string field, FilterOperator @operator, object? value)
    {
        Field = field;
        Operator = @operator;
        Value = value;
    }

    public string Field { get; }

    public FilterOperator Operator { get; }

    public object? Value { get; }

    // Wire format: "field:Operator:value". Field and Operator must not contain ':'.
    // Value is rendered with invariant culture; complex values should be pre-formatted
    // (DateTimeOffset -> "o", IEnumerable -> caller-chosen delimiter).
    public string Encode() =>
        $"{Field}:{Operator}:{FormatValue(Value)}";

    public static FilterModel Decode(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            throw new ArgumentException("Filter cannot be empty.", nameof(encoded));
        }

        var parts = encoded.Split(':', 3);
        if (parts.Length < 2)
        {
            throw new FormatException($"Filter '{encoded}' is not in 'field:operator[:value]' format.");
        }

        if (!Enum.TryParse<FilterOperator>(parts[1], ignoreCase: true, out var op))
        {
            throw new FormatException($"Unknown filter operator '{parts[1]}'.");
        }

        var value = parts.Length == 3 ? parts[2] : null;
        return new FilterModel(parts[0], op, value);
    }

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}

public enum FilterOperator
{
    Equal = 0,
    NotEqual = 1,
    GreaterThan = 2,
    GreaterThanOrEqual = 3,
    LessThan = 4,
    LessThanOrEqual = 5,
    Contains = 6,
    StartsWith = 7,
    EndsWith = 8,
    In = 9,
    NotIn = 10
}
