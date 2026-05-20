using DomainServices.Core.Models;

namespace DomainServices.Core.Services;

public interface IDomainServiceClient<TModel> : IReadOnlyDomainServiceClient<TModel>, IDomainService<TModel>
    where TModel : class, ICoreDomainModel
{
}
