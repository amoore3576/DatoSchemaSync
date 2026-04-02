namespace DatoSchemaSync.Models;

public class IdMapping
{
    // Maps source ID → destination ID for item types
    public Dictionary<string, string> ItemTypeIds { get; set; } = new();
    // Maps source ID → destination ID for fields
    public Dictionary<string, string> FieldIds { get; set; } = new();
    // Maps source locale code → destination locale ID
    public Dictionary<string, string> LocaleIds { get; set; } = new();
}
