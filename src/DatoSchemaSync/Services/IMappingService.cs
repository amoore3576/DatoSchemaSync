using DatoSchemaSync.Models;

namespace DatoSchemaSync.Services;

public interface IMappingService
{
    Task<IdMapping> LoadMappingAsync(CancellationToken cancellationToken = default);
    Task SaveMappingAsync(IdMapping mapping, CancellationToken cancellationToken = default);
}
