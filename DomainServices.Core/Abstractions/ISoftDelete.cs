namespace DomainServices.Core.Abstractions;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    void SetDelete(string userName);
}
