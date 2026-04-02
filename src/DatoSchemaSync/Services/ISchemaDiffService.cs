using DatoSchemaSync.Models;

namespace DatoSchemaSync.Services;

public interface ISchemaDiffService
{
    SchemaDiff ComputeDiff(DatoSchema previous, DatoSchema current);
}
