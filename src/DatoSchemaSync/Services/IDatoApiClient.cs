using DatoSchemaSync.Models;

namespace DatoSchemaSync.Services;

public interface IDatoApiClient
{
    Task<DatoSchema> FetchSchemaAsync(string apiToken, CancellationToken cancellationToken = default);
    Task<DatoItemType> CreateItemTypeAsync(string apiToken, DatoItemTypeAttributes attributes, CancellationToken cancellationToken = default);
    Task<DatoItemType> UpdateItemTypeAsync(string apiToken, string itemTypeId, DatoItemTypeAttributes attributes, CancellationToken cancellationToken = default);
    Task DeleteItemTypeAsync(string apiToken, string itemTypeId, CancellationToken cancellationToken = default);
    Task<DatoField> CreateFieldAsync(string apiToken, string itemTypeId, DatoFieldAttributes attributes, CancellationToken cancellationToken = default);
    Task<DatoField> UpdateFieldAsync(string apiToken, string fieldId, DatoFieldAttributes attributes, CancellationToken cancellationToken = default);
    Task DeleteFieldAsync(string apiToken, string fieldId, CancellationToken cancellationToken = default);
    Task CreateLocaleAsync(string apiToken, string localeCode, CancellationToken cancellationToken = default);
    Task DeleteLocaleAsync(string apiToken, string localeId, CancellationToken cancellationToken = default);
}
