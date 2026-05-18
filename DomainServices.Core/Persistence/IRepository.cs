using DomainServices.Core.Models;
using DomainServices.Core.Query;

namespace DomainServices.Core.Persistence;

public interface IRepository<TModel> where TModel : class, ICoreDomainModel
{
    Task<TModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TModel>> GetAllAsync(
        Guid enterpriseId,
        QueryParameterModel? query = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TModel>> SearchAllAsync(
        Guid enterpriseId,
        string searchTerm,
        QueryParameterModel? query = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TModel> AddAsync(TModel model, CancellationToken cancellationToken = default);

    Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> SaveAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default);
}
