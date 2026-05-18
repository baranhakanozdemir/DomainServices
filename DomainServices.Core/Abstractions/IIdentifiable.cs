namespace DomainServices.Core.Abstractions;

public interface IIdentifiable
{
    Guid Id { get; set; }

    void SetId(Guid id);
}
