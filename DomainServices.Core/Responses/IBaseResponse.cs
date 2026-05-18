using System.Net;

namespace DomainServices.Core.Responses;

public interface IBaseResponse
{
    bool IsSuccessful { get; }

    HttpStatusCode StatusCode { get; }

    string? Message { get; }

    IReadOnlyList<string> Errors { get; }

    Guid CorrelationId { get; }
}

public interface IBaseResponse<T> : IBaseResponse
{
    T? Data { get; }
}
