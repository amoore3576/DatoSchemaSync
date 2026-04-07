using DatoSchemaSync.Models;

namespace DatoSchemaSync.Services;

public interface IStorageService
{
    Task<DatoSchema?> LoadSnapshotAsync(CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(DatoSchema schema, CancellationToken cancellationToken = default);
    Task<IdMapping> LoadMappingAsync(CancellationToken cancellationToken = default);
    Task SaveMappingAsync(IdMapping mapping, CancellationToken cancellationToken = default);
}
