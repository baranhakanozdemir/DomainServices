using DomainServices.Core.Models;

namespace DomainServices.Core.Services;

public interface IServiceClient<TModel> : IReadOnlyServiceClient<TModel>, IDomainService<TModel>
    where TModel : class, ICoreDomainModel
{
}
