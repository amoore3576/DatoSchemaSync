using DatoSchemaSync.Models;

namespace DatoSchemaSync.Services;

public interface ISnapshotService
{
    Task<DatoSchema?> LoadSnapshotAsync(CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(DatoSchema schema, CancellationToken cancellationToken = default);
}
