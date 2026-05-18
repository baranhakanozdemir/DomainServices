using DomainServices.Core.Models;
using DomainServices.Core.Query;
using DomainServices.Core.Responses;
using DomainServices.Core.Validation;

namespace DomainServices.Core.Services;

public interface IDomainService<TModel> where TModel : class, ICoreDomainModel
{
    // Auth
    void SetToken(string token);

    // Sync read
    IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId);

    IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId, QueryParameterModel query);

    IBaseResponse<IReadOnlyList<TModel>> SearchAll(Guid enterpriseId, string searchTerm, QueryParameterModel? query = null);

    IBaseResponse<TModel> Get(Guid id);

    IBaseResponse<bool> CheckIfExists(Guid id);

    // Sync write
    IBaseResponse<TModel> Add(Guid enterpriseId, TModel model, string userName);

    IBaseResponse<TModel> Update(Guid id, TModel model, string userName);

    IBaseResponse Delete(Guid id, string userName);

    IBaseResponse<int> Save(Guid enterpriseId, ICollection<TModel> models, string userName);

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

    // Async write
    Task<IBaseResponse<TModel>> AddAsync(
        Guid enterpriseId,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default);

    Task<IBaseResponse<TModel>> UpdateAsync(
        Guid id,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default);

    Task<IBaseResponse> DeleteAsync(
        Guid id,
        string userName,
        CancellationToken cancellationToken = default);

    Task<IBaseResponse<int>> SaveAsync(
        Guid enterpriseId,
        ICollection<TModel> models,
        string userName,
        CancellationToken cancellationToken = default);

    // Validation
    ModelValidator Validate(TModel model);

    ModelValidator ValidateUpdate(Guid id, TModel model);

    ModelValidator ValidateDelete(Guid id);
}
