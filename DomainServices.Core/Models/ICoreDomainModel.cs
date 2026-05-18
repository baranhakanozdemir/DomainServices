using DomainServices.Core.Abstractions;

namespace DomainServices.Core.Models;

public interface ICoreDomainModel : ICoreModel, IAuditable, ISoftDelete, ITenantScoped, IValidatable
{
}
