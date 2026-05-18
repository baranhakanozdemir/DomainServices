namespace DomainServices.Core.Query;

public class QueryParameterModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? OrderBy { get; set; }

    public bool OrderDescending { get; set; }

    public string? SearchTerm { get; set; }

    public List<FilterModel> Filters { get; set; } = new();

    public int Skip => Math.Max(0, (Page - 1) * PageSize);

    public int Take => Math.Max(1, PageSize);
}
