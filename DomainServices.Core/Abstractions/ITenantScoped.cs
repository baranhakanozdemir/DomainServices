namespace DomainServices.Core.Abstractions;

public interface ITenantScoped
{
    Guid EnterpriseId { get; set; }
}
