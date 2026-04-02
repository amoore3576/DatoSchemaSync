using DatoSchemaSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatoSchemaSync.Services;

public class SchemaApplier : ISchemaApplier
{
    private readonly IDatoApiClient _apiClient;
    private readonly ILogger<SchemaApplier> _logger;
    private readonly SyncConfiguration _config;

    public SchemaApplier(IDatoApiClient apiClient, ILogger<SchemaApplier> logger, IOptions<SyncConfiguration> config)
    {
        _apiClient = apiClient;
        _logger = logger;
        _config = config.Value;
    }

    public async Task ApplyDiffAsync(SchemaDiff diff, DatoSchema currentSourceSchema, IdMapping mapping, CancellationToken cancellationToken = default)
    {
        if (!diff.HasChanges)
        {
            _logger.LogInformation("No changes to apply.");
            return;
        }

        if (_config.DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would apply: +{AL} -{RL} locales, +{AIT} ~{MIT} -{RIT} item types, +{AF} ~{MF} -{RF} fields",
                diff.AddedLocales.Count, diff.RemovedLocales.Count,
                diff.AddedItemTypes.Count, diff.ModifiedItemTypes.Count, diff.RemovedItemTypes.Count,
                diff.AddedFields.Count, diff.ModifiedFields.Count, diff.RemovedFields.Count);
            return;
        }

        var token = _config.DestinationApiToken;

        // 1. Apply locales first
        await ApplyLocalesAsync(diff, mapping, token, cancellationToken);

        // 2. Apply item types (models) - added, then modified
        await ApplyItemTypesAsync(diff, mapping, token, cancellationToken);

        // 3. Apply fields - added, then modified
        await ApplyFieldsAsync(diff, currentSourceSchema, mapping, token, cancellationToken);

        // 4. Remove fields (in reverse dependency order)
        await RemoveFieldsAsync(diff, mapping, token, cancellationToken);

        // 5. Remove item types
        await RemoveItemTypesAsync(diff, mapping, token, cancellationToken);

        // 6. Remove locales last
        await RemoveLocalesAsync(diff, mapping, token, cancellationToken);
    }

    private async Task ApplyLocalesAsync(SchemaDiff diff, IdMapping mapping, string token, CancellationToken cancellationToken)
    {
        foreach (var locale in diff.AddedLocales)
        {
            _logger.LogInformation("Adding locale: {Locale}", locale.Attributes.Locale);
            await _apiClient.CreateLocaleAsync(token, locale.Attributes.Locale, cancellationToken);
            mapping.LocaleIds[locale.Attributes.Locale] = locale.Attributes.Locale;
        }
    }

    private async Task ApplyItemTypesAsync(SchemaDiff diff, IdMapping mapping, string token, CancellationToken cancellationToken)
    {
        foreach (var itemType in diff.AddedItemTypes)
        {
            _logger.LogInformation("Creating item type: {ApiKey}", itemType.Attributes.ApiKey);
            var created = await _apiClient.CreateItemTypeAsync(token, itemType.Attributes, cancellationToken);
            mapping.ItemTypeIds[itemType.Id] = created.Id;
        }

        foreach (var itemType in diff.ModifiedItemTypes)
        {
            if (!mapping.ItemTypeIds.TryGetValue(itemType.Id, out var destId))
            {
                _logger.LogWarning("No destination mapping found for item type {Id}, skipping update", itemType.Id);
                continue;
            }
            _logger.LogInformation("Updating item type: {ApiKey}", itemType.Attributes.ApiKey);
            await _apiClient.UpdateItemTypeAsync(token, destId, itemType.Attributes, cancellationToken);
        }
    }

    private async Task ApplyFieldsAsync(SchemaDiff diff, DatoSchema currentSourceSchema, IdMapping mapping, string token, CancellationToken cancellationToken)
    {
        foreach (var field in diff.AddedFields)
        {
            var srcItemTypeId = field.Relationships?.ItemType?.Data?.Id;
            if (srcItemTypeId == null)
            {
                _logger.LogWarning("Field {FieldId} has no item type relationship, skipping", field.Id);
                continue;
            }

            if (!mapping.ItemTypeIds.TryGetValue(srcItemTypeId, out var destItemTypeId))
            {
                _logger.LogWarning("No destination mapping for item type {ItemTypeId}, cannot create field {FieldId}", srcItemTypeId, field.Id);
                continue;
            }

            _logger.LogInformation("Creating field: {ApiKey} on item type {ItemTypeId}", field.Attributes.ApiKey, destItemTypeId);
            var created = await _apiClient.CreateFieldAsync(token, destItemTypeId, field.Attributes, cancellationToken);
            mapping.FieldIds[field.Id] = created.Id;
        }

        foreach (var field in diff.ModifiedFields)
        {
            if (!mapping.FieldIds.TryGetValue(field.Id, out var destFieldId))
            {
                _logger.LogWarning("No destination mapping found for field {Id}, skipping update", field.Id);
                continue;
            }
            _logger.LogInformation("Updating field: {ApiKey}", field.Attributes.ApiKey);
            await _apiClient.UpdateFieldAsync(token, destFieldId, field.Attributes, cancellationToken);
        }
    }

    private async Task RemoveFieldsAsync(SchemaDiff diff, IdMapping mapping, string token, CancellationToken cancellationToken)
    {
        foreach (var field in diff.RemovedFields)
        {
            if (!mapping.FieldIds.TryGetValue(field.Id, out var destFieldId))
            {
                _logger.LogWarning("No destination mapping for field {Id}, skipping deletion", field.Id);
                continue;
            }
            _logger.LogInformation("Deleting field: {ApiKey}", field.Attributes.ApiKey);
            await _apiClient.DeleteFieldAsync(token, destFieldId, cancellationToken);
            mapping.FieldIds.Remove(field.Id);
        }
    }

    private async Task RemoveItemTypesAsync(SchemaDiff diff, IdMapping mapping, string token, CancellationToken cancellationToken)
    {
        foreach (var itemType in diff.RemovedItemTypes)
        {
            if (!mapping.ItemTypeIds.TryGetValue(itemType.Id, out var destId))
            {
                _logger.LogWarning("No destination mapping for item type {Id}, skipping deletion", itemType.Id);
                continue;
            }
            _logger.LogInformation("Deleting item type: {ApiKey}", itemType.Attributes.ApiKey);
            await _apiClient.DeleteItemTypeAsync(token, destId, cancellationToken);
            mapping.ItemTypeIds.Remove(itemType.Id);
        }
    }

    private async Task RemoveLocalesAsync(SchemaDiff diff, IdMapping mapping, string token, CancellationToken cancellationToken)
    {
        foreach (var locale in diff.RemovedLocales)
        {
            if (!mapping.LocaleIds.TryGetValue(locale.Attributes.Locale, out var destLocaleId))
            {
                _logger.LogWarning("No destination mapping for locale {Locale}, skipping deletion", locale.Attributes.Locale);
                continue;
            }
            _logger.LogInformation("Deleting locale: {Locale}", locale.Attributes.Locale);
            await _apiClient.DeleteLocaleAsync(token, destLocaleId, cancellationToken);
            mapping.LocaleIds.Remove(locale.Attributes.Locale);
        }
    }
}
