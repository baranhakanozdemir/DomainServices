using DomainServices.Core.Validation;

namespace DomainServices.Core.Models;

public abstract class CoreDomainModel : ICoreDomainModel
{
    public Guid Id { get; set; }

    public Guid EnterpriseId { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset? Updated { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual void SetId(Guid id) => Id = id;

    public virtual void SetCreate(string userName)
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
        Created = DateTimeOffset.UtcNow;
        CreatedBy = userName;
    }

    public virtual void SetUpdate(string userName)
    {
        Updated = DateTimeOffset.UtcNow;
        UpdatedBy = userName;
    }

    public virtual void SetDelete(string userName)
    {
        IsDeleted = true;
        SetUpdate(userName);
    }

    public virtual ModelValidator Validate()
    {
        var validator = new ModelValidator();
        validator.RequireNotEmpty(EnterpriseId, nameof(EnterpriseId));
        OnValidate(validator);
        return validator;
    }

    protected virtual void OnValidate(ModelValidator validator)
    {
    }
}
