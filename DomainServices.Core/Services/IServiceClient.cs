using DomainServices.Core.Models;

namespace DomainServices.Core.Services;

public interface IServiceClient<TModel> : IDomainService<TModel>
    where TModel : class, ICoreDomainModel
{
    Uri BaseAddress { get; }
}
