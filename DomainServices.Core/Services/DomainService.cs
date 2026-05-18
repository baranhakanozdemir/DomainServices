using System.Net;
using DomainServices.Core.Models;
using DomainServices.Core.Persistence;
using DomainServices.Core.Query;
using DomainServices.Core.Responses;
using DomainServices.Core.Validation;

namespace DomainServices.Core.Services;

public abstract class DomainService<TModel> : IDomainService<TModel>
    where TModel : class, ICoreDomainModel
{
    protected DomainService(IRepository<TModel> repository)
    {
        Repository = repository;
    }

    protected IRepository<TModel> Repository { get; }

    protected string? Token { get; private set; }

    public virtual void SetToken(string token) => Token = token;

    // -------- Sync wrappers over async (override for true-sync data stores) --------

    public virtual IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId) =>
        GetAllAsync(enterpriseId).GetAwaiter().GetResult();

    public virtual IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId, QueryParameterModel query) =>
        GetAllAsync(enterpriseId, query).GetAwaiter().GetResult();

    public virtual IBaseResponse<IReadOnlyList<TModel>> SearchAll(Guid enterpriseId, string searchTerm, QueryParameterModel? query = null) =>
        SearchAllAsync(enterpriseId, searchTerm, query).GetAwaiter().GetResult();

    public virtual IBaseResponse<TModel> Get(Guid id) =>
        GetAsync(id).GetAwaiter().GetResult();

    public virtual IBaseResponse<bool> CheckIfExists(Guid id) =>
        CheckIfExistsAsync(id).GetAwaiter().GetResult();

    public virtual IBaseResponse<TModel> Add(Guid enterpriseId, TModel model, string userName) =>
        AddAsync(enterpriseId, model, userName).GetAwaiter().GetResult();

    public virtual IBaseResponse<TModel> Update(Guid id, TModel model, string userName) =>
        UpdateAsync(id, model, userName).GetAwaiter().GetResult();

    public virtual IBaseResponse Delete(Guid id, string userName) =>
        DeleteAsync(id, userName).GetAwaiter().GetResult();

    public virtual IBaseResponse<int> Save(Guid enterpriseId, ICollection<TModel> models, string userName) =>
        SaveAsync(enterpriseId, models, userName).GetAwaiter().GetResult();

    // -------- Async --------

    public virtual async Task<IBaseResponse<IReadOnlyList<TModel>>> GetAllAsync(
        Guid enterpriseId,
        CancellationToken cancellationToken = default)
    {
        var result = await Repository.GetAllAsync(enterpriseId, null, cancellationToken).ConfigureAwait(false);
        return BaseResponse<IReadOnlyList<TModel>>.Ok(result);
    }

    public virtual async Task<IBaseResponse<IReadOnlyList<TModel>>> GetAllAsync(
        Guid enterpriseId,
        QueryParameterModel query,
        CancellationToken cancellationToken = default)
    {
        var result = await Repository.GetAllAsync(enterpriseId, query, cancellationToken).ConfigureAwait(false);
        return BaseResponse<IReadOnlyList<TModel>>.Ok(result);
    }

    public virtual async Task<IBaseResponse<IReadOnlyList<TModel>>> SearchAllAsync(
        Guid enterpriseId,
        string searchTerm,
        QueryParameterModel? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Repository.SearchAllAsync(searchTerm, query, cancellationToken).ConfigureAwait(false);
        return BaseResponse<IReadOnlyList<TModel>>.Ok(result);
    }

    public virtual async Task<IBaseResponse<TModel>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await Repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return model is null
            ? BaseResponse<TModel>.NotFound($"{typeof(TModel).Name} '{id}' not found.")
            : BaseResponse<TModel>.Ok(model);
    }

    public virtual async Task<IBaseResponse<bool>> CheckIfExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await Repository.ExistsAsync(id, cancellationToken).ConfigureAwait(false);
        return BaseResponse<bool>.Ok(exists);
    }

    public virtual async Task<IBaseResponse<TModel>> AddAsync(
        Guid enterpriseId,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        model.EnterpriseId = enterpriseId;
        model.SetCreate(userName);

        var validator = Validate(model);
        if (!validator.IsValid)
        {
            return BaseResponse<TModel>.BadRequest(validator.ErrorMessages, validator.Errors.Select(e => e.Message));
        }

        var created = await Repository.AddAsync(model, cancellationToken).ConfigureAwait(false);
        return BaseResponse<TModel>.Created(created);
    }

    public virtual async Task<IBaseResponse<TModel>> UpdateAsync(
        Guid id,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        model.SetId(id);
        model.SetUpdate(userName);

        var validator = ValidateUpdate(id, model);
        if (!validator.IsValid)
        {
            return BaseResponse<TModel>.BadRequest(validator.ErrorMessages, validator.Errors.Select(e => e.Message));
        }

        var existing = await Repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return BaseResponse<TModel>.NotFound($"{typeof(TModel).Name} '{id}' not found.");
        }

        var updated = await Repository.UpdateAsync(model, cancellationToken).ConfigureAwait(false);
        return BaseResponse<TModel>.Updated(updated);
    }

    public virtual async Task<IBaseResponse> DeleteAsync(
        Guid id,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var validator = ValidateDelete(id);
        if (!validator.IsValid)
        {
            return BaseResponse.BadRequest(validator.ErrorMessages, validator.Errors.Select(e => e.Message));
        }

        var existing = await Repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return BaseResponse.NotFound($"{typeof(TModel).Name} '{id}' not found.");
        }

        existing.SetDelete(userName);
        await Repository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return new BaseResponse(HttpStatusCode.NoContent);
    }

    public virtual async Task<IBaseResponse<int>> SaveAsync(
        Guid enterpriseId,
        ICollection<TModel> models,
        string userName,
        CancellationToken cancellationToken = default)
    {
        foreach (var model in models)
        {
            model.EnterpriseId = enterpriseId;
            if (model.Id == Guid.Empty)
            {
                model.SetCreate(userName);
            }
            else
            {
                model.SetUpdate(userName);
            }
        }

        var count = await Repository.SaveAsync(models, cancellationToken).ConfigureAwait(false);
        return BaseResponse<int>.Ok(count);
    }

    // -------- Validation hooks --------

    public virtual ModelValidator Validate(TModel model)
    {
        var validator = model.Validate();
        OnValidate(model, validator);
        return validator;
    }

    public virtual ModelValidator ValidateUpdate(Guid id, TModel model)
    {
        var validator = Validate(model);
        validator.Require(id != Guid.Empty, nameof(id), "Id is required for update.");
        OnValidateUpdate(id, model, validator);
        return validator;
    }

    public virtual ModelValidator ValidateDelete(Guid id)
    {
        var validator = new ModelValidator();
        validator.Require(id != Guid.Empty, nameof(id), "Id is required for delete.");
        OnValidateDelete(id, validator);
        return validator;
    }

    protected virtual void OnValidate(TModel model, ModelValidator validator) { }

    protected virtual void OnValidateUpdate(Guid id, TModel model, ModelValidator validator) { }

    protected virtual void OnValidateDelete(Guid id, ModelValidator validator) { }
}
