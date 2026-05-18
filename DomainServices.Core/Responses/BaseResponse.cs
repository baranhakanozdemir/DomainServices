using System.Net;

namespace DomainServices.Core.Responses;

public class BaseResponse : IBaseResponse
{
    public BaseResponse(
        HttpStatusCode statusCode,
        string? message = null,
        IEnumerable<string>? errors = null,
        Guid? correlationId = null)
    {
        StatusCode = statusCode;
        Message = message;
        Errors = errors?.ToList() ?? new List<string>();
        CorrelationId = correlationId ?? Guid.NewGuid();
    }

    public bool IsSuccessful => (int)StatusCode is >= 200 and < 300;

    public HttpStatusCode StatusCode { get; }

    public string? Message { get; }

    public IReadOnlyList<string> Errors { get; }

    public Guid CorrelationId { get; }

    public static BaseResponse Ok(string? message = null) =>
        new(HttpStatusCode.OK, message);

    public static BaseResponse NoContent() =>
        new(HttpStatusCode.NoContent);

    public static BaseResponse NotFound(string? message = null) =>
        new(HttpStatusCode.NotFound, message ?? "Resource not found.");

    public static BaseResponse BadRequest(string? message = null, IEnumerable<string>? errors = null) =>
        new(HttpStatusCode.BadRequest, message ?? "Bad request.", errors);

    public static BaseResponse Unauthorized(string? message = null) =>
        new(HttpStatusCode.Unauthorized, message ?? "Unauthorized.");

    public static BaseResponse Forbidden(string? message = null) =>
        new(HttpStatusCode.Forbidden, message ?? "Forbidden.");

    public static BaseResponse Conflict(string? message = null) =>
        new(HttpStatusCode.Conflict, message ?? "Conflict.");

    public static BaseResponse ServerError(string? message = null) =>
        new(HttpStatusCode.InternalServerError, message ?? "Internal server error.");
}

public class BaseResponse<T> : BaseResponse, IBaseResponse<T>
{
    public BaseResponse(
        HttpStatusCode statusCode,
        T? data = default,
        string? message = null,
        IEnumerable<string>? errors = null,
        Guid? correlationId = null)
        : base(statusCode, message, errors, correlationId)
    {
        Data = data;
    }

    public T? Data { get; }

    public static BaseResponse<T> Ok(T data, string? message = null) =>
        new(HttpStatusCode.OK, data, message);

    public static BaseResponse<T> Created(T data, string? message = null) =>
        new(HttpStatusCode.Created, data, message);

    public static BaseResponse<T> Updated(T data, string? message = null) =>
        new(HttpStatusCode.OK, data, message);

    public static new BaseResponse<T> NotFound(string? message = null) =>
        new(HttpStatusCode.NotFound, default, message ?? "Resource not found.");

    public static new BaseResponse<T> BadRequest(string? message = null, IEnumerable<string>? errors = null) =>
        new(HttpStatusCode.BadRequest, default, message ?? "Bad request.", errors);

    public static new BaseResponse<T> Unauthorized(string? message = null) =>
        new(HttpStatusCode.Unauthorized, default, message ?? "Unauthorized.");

    public static new BaseResponse<T> Forbidden(string? message = null) =>
        new(HttpStatusCode.Forbidden, default, message ?? "Forbidden.");

    public static new BaseResponse<T> Conflict(string? message = null) =>
        new(HttpStatusCode.Conflict, default, message ?? "Conflict.");

    public static new BaseResponse<T> ServerError(string? message = null) =>
        new(HttpStatusCode.InternalServerError, default, message ?? "Internal server error.");
}
