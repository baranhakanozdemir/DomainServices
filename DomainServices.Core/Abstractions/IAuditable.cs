namespace DomainServices.Core.Abstractions;

public interface IAuditable
{
    DateTimeOffset Created { get; set; }

    DateTimeOffset? Updated { get; set; }

    string CreatedBy { get; set; }

    string? UpdatedBy { get; set; }

    void SetCreate(string userName);

    void SetUpdate(string userName);
}
