using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DomainServices.Core.Models;
using DomainServices.Core.Query;
using DomainServices.Core.Responses;

namespace DomainServices.Core.Services;

public class ReadOnlyDomainServiceClient<TModel> : IReadOnlyDomainServiceClient<TModel>
    where TModel : class, ICoreDomainModel
{
    protected static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected HttpClient HttpClient { get; }

    protected JsonSerializerOptions JsonOptions { get; }

    protected string ResourcePath { get; }

    public ReadOnlyDomainServiceClient(HttpClient httpClient, string? resourcePath = null, JsonSerializerOptions? jsonOptions = null)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        JsonOptions = jsonOptions ?? DefaultJsonOptions;
        ResourcePath = NormalizeResourcePath(resourcePath ?? typeof(TModel).Name);
    }

    public Uri BaseAddress => HttpClient.BaseAddress
        ?? throw new InvalidOperationException("HttpClient.BaseAddress is not configured.");

    public virtual void SetToken(string token)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
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
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return BaseResponse<bool>.Ok(false);
        }

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync<bool>(response, cancellationToken).ConfigureAwait(false);
        }

        var exists = await response.Content.ReadFromJsonAsync<bool>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return BaseResponse<bool>.Ok(exists);
    }

    // -------- Protected helpers (shared with DomainServiceClient<T>) --------

    protected virtual string BuildUrl(string? segment, QueryParameterModel? query)
    {
        var path = string.IsNullOrEmpty(segment) ? ResourcePath : $"{ResourcePath}/{segment}";
        if (query is null)
        {
            return path;
        }

        var queryString = BuildQueryString(query);
        return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
    }

    protected static string BuildQueryString(QueryParameterModel query)
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

    protected async Task<IBaseResponse<IReadOnlyList<TModel>>> SendListAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return await BuildErrorAsync<IReadOnlyList<TModel>>(response, cancellationToken).ConfigureAwait(false);
        }

        var data = await response.Content
            .ReadFromJsonAsync<List<TModel>>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return BaseResponse<IReadOnlyList<TModel>>.Ok(data ?? new List<TModel>());
    }

    protected async Task<IBaseResponse<TModel>> SendItemAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

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

        var data = await response.Content.ReadFromJsonAsync<TModel>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return new BaseResponse<TModel>(response.StatusCode, data);
    }

    protected static async Task<BaseResponse<T>> BuildErrorAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var message = await SafeReadAsync(response, cancellationToken).ConfigureAwait(false);
        return new BaseResponse<T>(response.StatusCode, default, message);
    }

    protected static async Task<BaseResponse> BuildErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var message = await SafeReadAsync(response, cancellationToken).ConfigureAwait(false);
        return new BaseResponse(response.StatusCode, message);
    }

    protected static async Task<string?> SafeReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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
