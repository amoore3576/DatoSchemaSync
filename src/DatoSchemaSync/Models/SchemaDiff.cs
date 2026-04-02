namespace DatoSchemaSync.Models;

public class SchemaDiff
{
    public List<DatoLocale> AddedLocales { get; set; } = new();
    public List<DatoLocale> RemovedLocales { get; set; } = new();

    public List<DatoItemType> AddedItemTypes { get; set; } = new();
    public List<DatoItemType> ModifiedItemTypes { get; set; } = new();
    public List<DatoItemType> RemovedItemTypes { get; set; } = new();

    public List<DatoField> AddedFields { get; set; } = new();
    public List<DatoField> ModifiedFields { get; set; } = new();
    public List<DatoField> RemovedFields { get; set; } = new();

    public bool HasChanges =>
        AddedLocales.Count > 0 || RemovedLocales.Count > 0 ||
        AddedItemTypes.Count > 0 || ModifiedItemTypes.Count > 0 || RemovedItemTypes.Count > 0 ||
        AddedFields.Count > 0 || ModifiedFields.Count > 0 || RemovedFields.Count > 0;
}
