using DomainServices.Core.Models;
using DomainServices.Core.Persistence;
using DomainServices.Core.Responses;

namespace DomainServices.Core.Services;

/// <summary>
/// A <see cref="DomainService{TModel}"/> that records a best-effort audit entry after each
/// successful create / update / delete. Audit failures never break the CRUD operation that
/// already committed. The action vocabulary is domain-neutral: "Created" / "Updated" / "Deleted".
/// </summary>
public abstract class AuditedDomainService<TModel> : DomainService<TModel>
    where TModel : class, ICoreDomainModel
{
    /// <summary>Action strings recorded by this base. Kept as constants so consumers can match them.</summary>
    public const string ActionCreated = "Created";
    public const string ActionUpdated = "Updated";
    public const string ActionDeleted = "Deleted";

    private static readonly string EntityTypeName = typeof(TModel).Name;
    private readonly IAuditWriter _auditWriter;

    protected AuditedDomainService(IRepository<TModel> repository, IAuditWriter auditWriter)
        : base(repository)
    {
        _auditWriter = auditWriter;
    }

    protected AuditedDomainService(IRepository<TModel> repository)
        : this(repository, NullAuditWriter.Instance)
    {
    }

    public override IBaseResponse<TModel> Add(Guid enterpriseId, TModel model, string userName)
    {
        var response = base.Add(enterpriseId, model, userName);
        if (response.IsSuccessful)
        {
            TryWriteAudit(ResolveAuditActor(userName, response.Data?.CreatedBy, model.CreatedBy), ActionCreated, response.Data?.Id);
        }

        return response;
    }

    public override async Task<IBaseResponse<TModel>> AddAsync(
        Guid enterpriseId,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var response = await base.AddAsync(enterpriseId, model, userName, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            await TryWriteAuditAsync(
                ResolveAuditActor(userName, response.Data?.CreatedBy, model.CreatedBy),
                ActionCreated,
                response.Data?.Id,
                cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    public override IBaseResponse<TModel> Update(Guid id, TModel model, string userName)
    {
        var response = base.Update(id, model, userName);
        if (response.IsSuccessful)
        {
            TryWriteAudit(
                ResolveAuditActor(userName, response.Data?.UpdatedBy, model.UpdatedBy, model.CreatedBy),
                ActionUpdated,
                response.Data?.Id);
        }

        return response;
    }

    public override async Task<IBaseResponse<TModel>> UpdateAsync(
        Guid id,
        TModel model,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var response = await base.UpdateAsync(id, model, userName, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            await TryWriteAuditAsync(
                ResolveAuditActor(userName, response.Data?.UpdatedBy, model.UpdatedBy, model.CreatedBy),
                ActionUpdated,
                response.Data?.Id,
                cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    public override IBaseResponse Delete(Guid id, string userName)
    {
        var response = base.Delete(id, userName);
        if (response.IsSuccessful)
        {
            TryWriteAudit(ResolveAuditActor(userName), ActionDeleted, id);
        }

        return response;
    }

    public override async Task<IBaseResponse> DeleteAsync(
        Guid id,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var response = await base.DeleteAsync(id, userName, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            await TryWriteAuditAsync(ResolveAuditActor(userName), ActionDeleted, id, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    public override IBaseResponse<int> Save(Guid enterpriseId, ICollection<TModel> models, string userName)
    {
        var response = base.Save(enterpriseId, models, userName);
        if (response.IsSuccessful)
        {
            TryWriteBatchAudit(ResolveAuditActor(userName), models);
        }

        return response;
    }

    public override async Task<IBaseResponse<int>> SaveAsync(
        Guid enterpriseId,
        ICollection<TModel> models,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var response = await base.SaveAsync(enterpriseId, models, userName, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            await TryWriteBatchAuditAsync(ResolveAuditActor(userName), models, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private void TryWriteBatchAudit(string actor, IEnumerable<TModel>? models)
    {
        if (models is null)
            return;

        foreach (var model in models)
        {
            TryWriteAudit(actor, ActionUpdated, model.Id);
        }
    }

    private async Task TryWriteBatchAuditAsync(string actor, IEnumerable<TModel>? models, CancellationToken cancellationToken)
    {
        if (models is null)
            return;

        foreach (var model in models)
        {
            await TryWriteAuditAsync(actor, ActionUpdated, model.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private void TryWriteAudit(string actor, string action, Guid? entityId)
    {
        if (entityId is not { } id)
            return;

        TryWriteAuditAsync(actor, action, id, CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task TryWriteAuditAsync(string actor, string action, Guid? entityId, CancellationToken cancellationToken)
    {
        if (entityId is not { } id)
            return;

        try
        {
            await _auditWriter.WriteAsync(actor, action, EntityTypeName, id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Best-effort: audit failure must not break the CRUD operation that already committed.
        }
    }

    private static string ResolveAuditActor(params string?[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate)) ?? "system";
}
