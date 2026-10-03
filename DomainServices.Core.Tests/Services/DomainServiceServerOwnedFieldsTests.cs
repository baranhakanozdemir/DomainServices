using DomainServices.Core.Models;
using DomainServices.Core.Persistence;
using DomainServices.Core.Query;
using DomainServices.Core.Services;
using Xunit;

namespace DomainServices.Core.Tests.Services;

// Issue #20: an update must not soft-delete a row or rewrite who created it, whatever the
// caller sent. Before the fix UpdateAsync and SaveAsync kept only EnterpriseId from the
// stored row, so IsDeleted / Created / CreatedBy reached the repository as sent.
public class DomainServiceServerOwnedFieldsTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTimeOffset StoredCreated = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private const string StoredCreatedBy = "original-author";

    [Fact]
    public async Task UpdateAsync_KeepsStoredIsDeletedCreatedAndCreatedBy()
    {
        var repository = new RecordingRepository();
        var stored = repository.Seed(StoredRow());
        var service = new WidgetService(repository);

        var response = await service.UpdateAsync(stored.Id, SpoofedUpdate(stored.Id), "editor");

        Assert.True(response.IsSuccessful);
        var written = Assert.Single(repository.Updated);
        Assert.False(written.IsDeleted);
        Assert.Equal(StoredCreated, written.Created);
        Assert.Equal(StoredCreatedBy, written.CreatedBy);
        Assert.Equal("renamed", written.Name);
        Assert.Equal("editor", written.UpdatedBy);
        Assert.NotNull(written.Updated);
    }

    [Fact]
    public async Task SaveAsync_ExistingRow_KeepsStoredIsDeletedCreatedAndCreatedBy()
    {
        var repository = new RecordingRepository();
        var stored = repository.Seed(StoredRow());
        var service = new WidgetService(repository);

        var response = await service.SaveAsync(Tenant, [SpoofedUpdate(stored.Id)], "editor");

        Assert.True(response.IsSuccessful);
        var written = Assert.Single(Assert.Single(repository.Saved));
        Assert.False(written.IsDeleted);
        Assert.Equal(StoredCreated, written.Created);
        Assert.Equal(StoredCreatedBy, written.CreatedBy);
        Assert.Equal("renamed", written.Name);
        Assert.Equal("editor", written.UpdatedBy);
    }

    // A consumer repairs a row whose creation record was never written by saving it through
    // UpdateAsync (plusteam ProjectController.Get -> ProjectAuditTimestamps.TryRepairMissingCreated).
    // The values it fills in must persist.
    [Fact]
    public async Task UpdateAsync_StoredCreationRecordNeverWritten_AcceptsTheRepair()
    {
        var repository = new RecordingRepository();
        var unwritten = StoredRow();
        unwritten.Created = default;
        unwritten.CreatedBy = string.Empty;
        var stored = repository.Seed(unwritten);
        var service = new WidgetService(repository);
        var repairedCreated = new DateTimeOffset(2025, 6, 7, 8, 9, 10, TimeSpan.Zero);

        var repair = Copy(stored);
        repair.Created = repairedCreated;
        repair.CreatedBy = "repaired-author";
        var response = await service.UpdateAsync(stored.Id, repair, "editor");

        Assert.True(response.IsSuccessful);
        var written = Assert.Single(repository.Updated);
        Assert.Equal(repairedCreated, written.Created);
        Assert.Equal("repaired-author", written.CreatedBy);
    }

    [Fact]
    public async Task DeleteAsync_StillSoftDeletes()
    {
        var repository = new RecordingRepository();
        var stored = repository.Seed(StoredRow());
        var service = new WidgetService(repository);

        await service.DeleteAsync(stored.Id, "editor");

        Assert.True(Assert.Single(repository.Updated).IsDeleted);
    }

    private static Widget StoredRow() => new()
    {
        Id = Guid.NewGuid(),
        EnterpriseId = Tenant,
        Name = "original",
        Created = StoredCreated,
        CreatedBy = StoredCreatedBy,
        IsDeleted = false,
    };

    // What a caller who binds the entity can send: a soft-delete and a forged creation record.
    private static Widget SpoofedUpdate(Guid id) => new()
    {
        Id = id,
        EnterpriseId = Tenant,
        Name = "renamed",
        Created = new DateTimeOffset(1999, 12, 31, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "forged-author",
        IsDeleted = true,
    };

    private static Widget Copy(Widget source) => new()
    {
        Id = source.Id,
        EnterpriseId = source.EnterpriseId,
        Name = source.Name,
        Created = source.Created,
        CreatedBy = source.CreatedBy,
        Updated = source.Updated,
        UpdatedBy = source.UpdatedBy,
        IsDeleted = source.IsDeleted,
    };

    private sealed class Widget : CoreDomainModel
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class WidgetService(IRepository<Widget> repository) : DomainService<Widget>(repository);

    // Stores copies, so what the service hands to UpdateAsync / SaveAsync is exactly what a real
    // repository would write, and GetAsync never returns the caller's own instance.
    private sealed class RecordingRepository : IRepository<Widget>
    {
        private readonly Dictionary<Guid, Widget> _rows = [];

        public List<Widget> Updated { get; } = [];

        public List<List<Widget>> Saved { get; } = [];

        public Widget Seed(Widget row)
        {
            _rows[row.Id] = Copy(row);
            return Copy(row);
        }

        public Task<Widget?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows.TryGetValue(id, out var row) ? Copy(row) : null);

        public Task<Widget> UpdateAsync(Widget model, CancellationToken cancellationToken = default)
        {
            Updated.Add(Copy(model));
            _rows[model.Id] = Copy(model);
            return Task.FromResult(Copy(model));
        }

        public Task<int> SaveAsync(IEnumerable<Widget> models, CancellationToken cancellationToken = default)
        {
            var batch = models.Select(Copy).ToList();
            Saved.Add(batch);
            return Task.FromResult(batch.Count);
        }

        public Task<IReadOnlyList<Widget>> GetAllAsync(Guid enterpriseId, QueryParameterModel? query = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Widget>> SearchAllAsync(Guid enterpriseId, string searchTerm, QueryParameterModel? query = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Widget> AddAsync(Widget model, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
