using DatoSchemaSync.Models;

namespace DatoSchemaSync.Services;

public interface ISchemaApplier
{
    Task ApplyDiffAsync(SchemaDiff diff, DatoSchema currentSourceSchema, IdMapping mapping, CancellationToken cancellationToken = default);
}
