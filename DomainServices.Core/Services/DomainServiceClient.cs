using System.Net.Http.Json;
using System.Text.Json;
using DomainServices.Core.Models;
using DomainServices.Core.Responses;
using DomainServices.Core.Validation;

namespace DomainServices.Core.Services;

public class DomainServiceClient<TModel> : ReadOnlyDomainServiceClient<TModel>, IDomainServiceClient<TModel>
    where TModel : class, ICoreDomainModel
{
    public DomainServiceClient(HttpClient httpClient, string? resourcePath = null, JsonSerializerOptions? jsonOptions = null)
        : base(httpClient, resourcePath, jsonOptions)
    {
    }

    // -------- Sync wrappers --------

    public IBaseResponse<TModel> Add(Guid enterpriseId, TModel model, string userName) =>
        AddAsync(enterpriseId, model, userName).GetAwaiter().GetResult();

    public IBaseResponse<TModel> Update(Guid id, TModel model, string userName) =>
        UpdateAsync(id, model, userName).GetAwaiter().GetResult();

    public IBaseResponse Delete(Guid id, string userName) =>
        DeleteAsync(id, userName).GetAwaiter().GetResult();

    public IBaseResponse<int> Save(Guid enterpriseId, ICollection<TModel> models, string userName) =>
        SaveAsync(enterpriseId, models, userName).GetAwaiter().GetResult();

    // -------- Async --------

    public virtual async Task<IBaseResponse<TModel>> AddAsync(
        Guid enterpriseId,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{enterpriseId}?userName={Uri.EscapeDataString(userName)}", query: null);
        var content = JsonContent.Create(model, options: JsonOptions);
        return await SendItemAsync(HttpMethod.Post, url, content, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IBaseResponse<TModel>> UpdateAsync(
        Guid id,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{id}?userName={Uri.EscapeDataString(userName)}", query: null);
        var content = JsonContent.Create(model, options: JsonOptions);
        return await SendItemAsync(HttpMethod.Put, url, content, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IBaseResponse> DeleteAsync(
        Guid id,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{id}?userName={Uri.EscapeDataString(userName)}", query: null);
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return new BaseResponse(response.StatusCode);
    }

    public virtual async Task<IBaseResponse<int>> SaveAsync(
        Guid enterpriseId,
        ICollection<TModel> models,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{enterpriseId}/batch?userName={Uri.EscapeDataString(userName)}", query: null);
        var content = JsonContent.Create(models, options: JsonOptions);
        using var response = await HttpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync<int>(response, cancellationToken).ConfigureAwait(false);
        }

        var count = await response.Content.ReadFromJsonAsync<int>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return BaseResponse<int>.Ok(count);
    }

    // -------- Validation: client-side defers to model --------

    public virtual ModelValidator Validate(TModel model) => model.Validate();

    public virtual ModelValidator ValidateUpdate(Guid id, TModel model)
    {
        var validator = Validate(model);
        validator.Require(id != Guid.Empty, nameof(id), "Id is required for update.");
        return validator;
    }

    public virtual ModelValidator ValidateDelete(Guid id)
    {
        var validator = new ModelValidator();
        validator.Require(id != Guid.Empty, nameof(id), "Id is required for delete.");
        return validator;
    }
}
