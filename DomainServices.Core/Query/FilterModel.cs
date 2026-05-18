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
