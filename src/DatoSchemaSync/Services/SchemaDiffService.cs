using System.Text.Json;
using DatoSchemaSync.Models;
using Microsoft.Extensions.Logging;

namespace DatoSchemaSync.Services;

public class SchemaDiffService : ISchemaDiffService
{
    private readonly ILogger<SchemaDiffService> _logger;

    public SchemaDiffService(ILogger<SchemaDiffService> logger)
    {
        _logger = logger;
    }

    public SchemaDiff ComputeDiff(DatoSchema previous, DatoSchema current)
    {
        var diff = new SchemaDiff();

        // Locales diff by locale code
        var prevLocales = previous.Locales.ToDictionary(l => l.Attributes.Locale);
        var currLocales = current.Locales.ToDictionary(l => l.Attributes.Locale);

        diff.AddedLocales = current.Locales.Where(l => !prevLocales.ContainsKey(l.Attributes.Locale)).ToList();
        diff.RemovedLocales = previous.Locales.Where(l => !currLocales.ContainsKey(l.Attributes.Locale)).ToList();

        // ItemTypes diff by api_key
        var prevItemTypes = previous.ItemTypes.ToDictionary(it => it.Attributes.ApiKey);
        var currItemTypes = current.ItemTypes.ToDictionary(it => it.Attributes.ApiKey);

        diff.AddedItemTypes = current.ItemTypes.Where(it => !prevItemTypes.ContainsKey(it.Attributes.ApiKey)).ToList();
        diff.RemovedItemTypes = previous.ItemTypes.Where(it => !currItemTypes.ContainsKey(it.Attributes.ApiKey)).ToList();
        diff.ModifiedItemTypes = current.ItemTypes
            .Where(it => prevItemTypes.TryGetValue(it.Attributes.ApiKey, out var prev) && !ItemTypeAttributesEqual(prev.Attributes, it.Attributes))
            .ToList();

        // Fields diff - by combination of item_type api_key + field api_key
        var prevFields = previous.Fields
            .Where(f => f.Relationships?.ItemType?.Data?.Id != null)
            .ToDictionary(f => GetFieldKey(f, previous));
        var currFields = current.Fields
            .Where(f => f.Relationships?.ItemType?.Data?.Id != null)
            .ToDictionary(f => GetFieldKey(f, current));

        diff.AddedFields = current.Fields.Where(f => !prevFields.ContainsKey(GetFieldKey(f, current))).ToList();
        diff.RemovedFields = previous.Fields.Where(f => !currFields.ContainsKey(GetFieldKey(f, previous))).ToList();
        diff.ModifiedFields = current.Fields
            .Where(f =>
            {
                var key = GetFieldKey(f, current);
                return prevFields.TryGetValue(key, out var prev) && !FieldAttributesEqual(prev.Attributes, f.Attributes);
            })
            .ToList();

        _logger.LogInformation("Diff: +{AL} -{RL} locales, +{AIT} ~{MIT} -{RIT} item types, +{AF} ~{MF} -{RF} fields",
            diff.AddedLocales.Count, diff.RemovedLocales.Count,
            diff.AddedItemTypes.Count, diff.ModifiedItemTypes.Count, diff.RemovedItemTypes.Count,
            diff.AddedFields.Count, diff.ModifiedFields.Count, diff.RemovedFields.Count);

        return diff;
    }

    private static string GetFieldKey(DatoField field, DatoSchema schema)
    {
        var itemTypeId = field.Relationships?.ItemType?.Data?.Id ?? string.Empty;
        var itemTypeApiKey = schema.ItemTypes.FirstOrDefault(it => it.Id == itemTypeId)?.Attributes.ApiKey ?? itemTypeId;
        return $"{itemTypeApiKey}.{field.Attributes.ApiKey}";
    }

    private static bool ItemTypeAttributesEqual(DatoItemTypeAttributes a, DatoItemTypeAttributes b)
    {
        return a.Name == b.Name &&
               a.Singleton == b.Singleton &&
               a.Sortable == b.Sortable &&
               a.ModularBlock == b.ModularBlock &&
               a.Tree == b.Tree &&
               a.OrderingDirection == b.OrderingDirection &&
               a.DraftModeActive == b.DraftModeActive &&
               a.AllLocalesRequired == b.AllLocalesRequired &&
               a.CollectionAppearance == b.CollectionAppearance;
    }

    private static bool FieldAttributesEqual(DatoFieldAttributes a, DatoFieldAttributes b)
    {
        return a.Label == b.Label &&
               a.FieldType == b.FieldType &&
               a.Hint == b.Hint &&
               a.Localized == b.Localized &&
               a.Position == b.Position &&
               JsonElementEqual(a.Validators, b.Validators) &&
               JsonElementEqual(a.Appearance, b.Appearance) &&
               JsonElementEqual(a.DefaultValue, b.DefaultValue);
    }

    private static bool JsonElementEqual(System.Text.Json.JsonElement? a, System.Text.Json.JsonElement? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Value.ToString() == b.Value.ToString();
    }
}
