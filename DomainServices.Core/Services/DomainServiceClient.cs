using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DomainServices.Core.Models;
using DomainServices.Core.Query;
using DomainServices.Core.Responses;
using DomainServices.Core.Validation;

namespace DomainServices.Core.Services;

public class DomainServiceClient<TModel> : IServiceClient<TModel>
    where TModel : class, ICoreDomainModel
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _resourcePath;

    public DomainServiceClient(HttpClient httpClient, string? resourcePath = null, JsonSerializerOptions? jsonOptions = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = jsonOptions ?? DefaultJsonOptions;
        _resourcePath = NormalizeResourcePath(resourcePath ?? typeof(TModel).Name);
    }

    public Uri BaseAddress => _httpClient.BaseAddress ?? throw new InvalidOperationException("HttpClient.BaseAddress is not configured.");

    public virtual void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // -------- Sync wrappers --------

    public IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId) =>
        GetAllAsync(enterpriseId).GetAwaiter().GetResult();

    public IBaseResponse<IReadOnlyList<TModel>> GetAll(Guid enterpriseId, QueryParameterModel query) =>
        GetAllAsync(enterpriseId, query).GetAwaiter().GetResult();

    public IBaseResponse<IReadOnlyList<TModel>> SearchAll(Guid enterpriseId, string searchTerm, QueryParameterModel? query = null) =>
        SearchAllAsync(enterpriseId, searchTerm, query).GetAwaiter().GetResult();

    public IBaseResponse<TModel> Get(Guid id) => GetAsync(id).GetAwaiter().GetResult();

    public IBaseResponse<bool> CheckIfExists(Guid id) => CheckIfExistsAsync(id).GetAwaiter().GetResult();

    public IBaseResponse<TModel> Add(Guid enterpriseId, TModel model, string userName) =>
        AddAsync(enterpriseId, model, userName).GetAwaiter().GetResult();

    public IBaseResponse<TModel> Update(Guid id, TModel model, string userName) =>
        UpdateAsync(id, model, userName).GetAwaiter().GetResult();

    public IBaseResponse Delete(Guid id, string userName) =>
        DeleteAsync(id, userName).GetAwaiter().GetResult();

    public IBaseResponse<int> Save(Guid enterpriseId, ICollection<TModel> models, string userName) =>
        SaveAsync(enterpriseId, models, userName).GetAwaiter().GetResult();

    // -------- Async --------

    public virtual Task<IBaseResponse<IReadOnlyList<TModel>>> GetAllAsync(
        Guid enterpriseId,
        CancellationToken cancellationToken = default) =>
        GetAllAsync(enterpriseId, new QueryParameterModel(), cancellationToken);

    public virtual async Task<IBaseResponse<IReadOnlyList<TModel>>> GetAllAsync(
        Guid enterpriseId,
        QueryParameterModel query,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(enterpriseId.ToString(), query);
        return await SendListAsync(HttpMethod.Get, url, content: null, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IBaseResponse<IReadOnlyList<TModel>>> SearchAllAsync(
        Guid enterpriseId,
        string searchTerm,
        QueryParameterModel? query = null,
        CancellationToken cancellationToken = default)
    {
        var effective = query?.Clone() ?? new QueryParameterModel();
        effective.SearchTerm = searchTerm;
        var url = BuildUrl($"{enterpriseId}/search", effective);
        return await SendListAsync(HttpMethod.Get, url, content: null, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IBaseResponse<TModel>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(id.ToString(), query: null);
        return await SendItemAsync(HttpMethod.Get, url, content: null, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IBaseResponse<bool>> CheckIfExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{id}/exists", query: null);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return BaseResponse<bool>.Ok(false);
        }

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync<bool>(response, cancellationToken).ConfigureAwait(false);
        }

        var exists = await response.Content.ReadFromJsonAsync<bool>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return BaseResponse<bool>.Ok(exists);
    }

    public virtual async Task<IBaseResponse<TModel>> AddAsync(
        Guid enterpriseId,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{enterpriseId}?userName={Uri.EscapeDataString(userName)}", query: null);
        var content = JsonContent.Create(model, options: _jsonOptions);
        return await SendItemAsync(HttpMethod.Post, url, content, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IBaseResponse<TModel>> UpdateAsync(
        Guid id,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{id}?userName={Uri.EscapeDataString(userName)}", query: null);
        var content = JsonContent.Create(model, options: _jsonOptions);
        return await SendItemAsync(HttpMethod.Put, url, content, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<IBaseResponse> DeleteAsync(
        Guid id,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{id}?userName={Uri.EscapeDataString(userName)}", query: null);
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

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
        var content = JsonContent.Create(models, options: _jsonOptions);
        using var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync<int>(response, cancellationToken).ConfigureAwait(false);
        }

        var count = await response.Content.ReadFromJsonAsync<int>(_jsonOptions, cancellationToken).ConfigureAwait(false);
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

    // -------- Helpers --------

    protected virtual string BuildUrl(string? segment, QueryParameterModel? query)
    {
        var path = string.IsNullOrEmpty(segment) ? _resourcePath : $"{_resourcePath}/{segment}";
        if (query is null)
        {
            return path;
        }

        var queryString = BuildQueryString(query);
        return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
    }

    private static string BuildQueryString(QueryParameterModel query)
    {
        var parts = new List<string>
        {
            $"page={query.Page}",
            $"pageSize={query.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            parts.Add($"orderBy={Uri.EscapeDataString(query.OrderBy)}");
            parts.Add($"orderDescending={query.OrderDescending.ToString().ToLowerInvariant()}");
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            parts.Add($"searchTerm={Uri.EscapeDataString(query.SearchTerm)}");
        }

        foreach (var filter in query.Filters)
        {
            parts.Add($"filter={Uri.EscapeDataString(filter.Encode())}");
        }

        return string.Join("&", parts);
    }

    private async Task<IBaseResponse<IReadOnlyList<TModel>>> SendListAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync<IReadOnlyList<TModel>>(response, cancellationToken).ConfigureAwait(false);
        }

        var data = await response.Content
            .ReadFromJsonAsync<List<TModel>>(_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return BaseResponse<IReadOnlyList<TModel>>.Ok(data ?? new List<TModel>());
    }

    private async Task<IBaseResponse<TModel>> SendItemAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return BaseResponse<TModel>.NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync<TModel>(response, cancellationToken).ConfigureAwait(false);
        }

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return new BaseResponse<TModel>(response.StatusCode, default);
        }

        var data = await response.Content.ReadFromJsonAsync<TModel>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new BaseResponse<TModel>(response.StatusCode, data);
    }

    private static async Task<BaseResponse<T>> BuildErrorAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var message = await SafeReadAsync(response, cancellationToken).ConfigureAwait(false);
        return new BaseResponse<T>(response.StatusCode, default, message);
    }

    private static async Task<BaseResponse> BuildErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var message = await SafeReadAsync(response, cancellationToken).ConfigureAwait(false);
        return new BaseResponse(response.StatusCode, message);
    }

    private static async Task<string?> SafeReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return response.ReasonPhrase;
        }
    }

    private static string NormalizeResourcePath(string path) =>
        path.Trim('/');
}
