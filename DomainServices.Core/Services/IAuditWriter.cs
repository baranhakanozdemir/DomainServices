namespace DomainServices.Core.Services;

/// <summary>
/// Writes an audit record for a domain action. The action is a free-form string so this base
/// library stays domain-neutral — consumers pass their own action vocabulary (e.g. an enum's
/// name). <see cref="AuditedDomainService{TModel}"/> records "Created" / "Updated" / "Deleted".
/// </summary>
public interface IAuditWriter
{
    Task WriteAsync(
        string actor,
        string action,
        string entityType,
        Guid? entityId = null,
        Guid? projectId = null,
        string? details = null,
        CancellationToken cancellationToken = default,
        Guid? enterpriseId = null);
}

/// <summary>No-op <see cref="IAuditWriter"/> for tests and contexts without an audit sink.</summary>
public sealed class NullAuditWriter : IAuditWriter
{
    public static readonly NullAuditWriter Instance = new();

    private NullAuditWriter()
    {
    }

    public Task WriteAsync(
        string actor,
        string action,
        string entityType,
        Guid? entityId = null,
        Guid? projectId = null,
        string? details = null,
        CancellationToken cancellationToken = default,
        Guid? enterpriseId = null) => Task.CompletedTask;
}
