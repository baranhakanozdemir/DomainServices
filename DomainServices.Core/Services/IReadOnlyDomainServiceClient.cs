using DomainServices.Core.Models;
using DomainServices.Core.Query;
using DomainServices.Core.Responses;

namespace DomainServices.Core.Services;

public interface IReadOnlyDomainServiceClient<TModel> where TModel : class, ICoreDomainModel
{
    Uri BaseAddress { get; }

    // Auth
    void SetToken(string token);

    // Sync read
    IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId);

    IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId, QueryParameterModel query);

    IBaseResponse<IReadOnlyList<TModel>> SearchAll(Guid enterpriseId, string searchTerm, QueryParameterModel? query = null);

    IBaseResponse<TModel> Get(Guid id);

    IBaseResponse<bool> CheckIfExists(Guid id);

    // Async read
    Task<IBaseResponse<IReadOnlyList<TModel>>> GetAllAsync(
        Guid enterpriseId,
        CancellationToken cancellationToken = default);

    Task<IBaseResponse<IReadOnlyList<TModel>>> GetAllAsync(
        Guid enterpriseId,
        QueryParameterModel query,
        CancellationToken cancellationToken = default);

    Task<IBaseResponse<IReadOnlyList<TModel>>> SearchAllAsync(
        Guid enterpriseId,
        string searchTerm,
        QueryParameterModel? query = null,
        CancellationToken cancellationToken = default);

    Task<IBaseResponse<TModel>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IBaseResponse<bool>> CheckIfExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
