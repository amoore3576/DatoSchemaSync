using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DatoSchemaSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatoSchemaSync.Services;

public class DatoApiClient : IDatoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DatoApiClient> _logger;
    private readonly SyncConfiguration _config;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public DatoApiClient(HttpClient httpClient, ILogger<DatoApiClient> logger, IOptions<SyncConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string apiToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-Api-Version", "3");
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        return request;
    }

    public async Task<DatoSchema> FetchSchemaAsync(string apiToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching DatoCMS schema...");

        var schema = new DatoSchema();

        // Fetch locales
        var siteResp = await SendAsync(BuildRequest(HttpMethod.Get, $"{_config.DatoApiBaseUrl}/site", apiToken), cancellationToken);
        var siteJson = await siteResp.Content.ReadAsStringAsync(cancellationToken);
        var siteDoc = JsonDocument.Parse(siteJson);
        if (siteDoc.RootElement.TryGetProperty("data", out var siteData) &&
            siteData.TryGetProperty("attributes", out var siteAttrs) &&
            siteAttrs.TryGetProperty("locales", out var localesArray))
        {
            foreach (var loc in localesArray.EnumerateArray())
            {
                var locCode = loc.GetString() ?? string.Empty;
                schema.Locales.Add(new DatoLocale
                {
                    Id = locCode,
                    Attributes = new DatoLocaleAttributes { Locale = locCode }
                });
            }
        }

        // Fetch item types
        var itemTypesResp = await SendAsync(BuildRequest(HttpMethod.Get, $"{_config.DatoApiBaseUrl}/item-types", apiToken), cancellationToken);
        var itemTypesJson = await itemTypesResp.Content.ReadAsStringAsync(cancellationToken);
        var itemTypesData = JsonSerializer.Deserialize<DatoListResponse<DatoItemType>>(itemTypesJson, JsonOptions);
        if (itemTypesData?.Data != null)
            schema.ItemTypes.AddRange(itemTypesData.Data);

        // Fetch all fields
        foreach (var itemType in schema.ItemTypes)
        {
            var fieldsResp = await SendAsync(BuildRequest(HttpMethod.Get, $"{_config.DatoApiBaseUrl}/item-types/{itemType.Id}/fields", apiToken), cancellationToken);
            var fieldsJson = await fieldsResp.Content.ReadAsStringAsync(cancellationToken);
            var fieldsData = JsonSerializer.Deserialize<DatoListResponse<DatoField>>(fieldsJson, JsonOptions);
            if (fieldsData?.Data != null)
                schema.Fields.AddRange(fieldsData.Data);
        }

        _logger.LogInformation("Fetched schema: {Locales} locales, {ItemTypes} item types, {Fields} fields",
            schema.Locales.Count, schema.ItemTypes.Count, schema.Fields.Count);

        return schema;
    }

    public async Task<DatoItemType> CreateItemTypeAsync(string apiToken, DatoItemTypeAttributes attributes, CancellationToken cancellationToken = default)
    {
        var body = new { data = new { type = "item_type", attributes } };
        var resp = await SendAsync(BuildRequest(HttpMethod.Post, $"{_config.DatoApiBaseUrl}/item-types", apiToken, body), cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<DatoSingleResponse<DatoItemType>>(json, JsonOptions);
        return result!.Data;
    }

    public async Task<DatoItemType> UpdateItemTypeAsync(string apiToken, string itemTypeId, DatoItemTypeAttributes attributes, CancellationToken cancellationToken = default)
    {
        var body = new { data = new { type = "item_type", id = itemTypeId, attributes } };
        var resp = await SendAsync(BuildRequest(HttpMethod.Put, $"{_config.DatoApiBaseUrl}/item-types/{itemTypeId}", apiToken, body), cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<DatoSingleResponse<DatoItemType>>(json, JsonOptions);
        return result!.Data;
    }

    public async Task DeleteItemTypeAsync(string apiToken, string itemTypeId, CancellationToken cancellationToken = default)
    {
        await SendAsync(BuildRequest(HttpMethod.Delete, $"{_config.DatoApiBaseUrl}/item-types/{itemTypeId}", apiToken), cancellationToken);
    }

    public async Task<DatoField> CreateFieldAsync(string apiToken, string itemTypeId, DatoFieldAttributes attributes, CancellationToken cancellationToken = default)
    {
        var body = new { data = new { type = "field", attributes, relationships = new { item_type = new { data = new { type = "item_type", id = itemTypeId } } } } };
        var resp = await SendAsync(BuildRequest(HttpMethod.Post, $"{_config.DatoApiBaseUrl}/item-types/{itemTypeId}/fields", apiToken, body), cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<DatoSingleResponse<DatoField>>(json, JsonOptions);
        return result!.Data;
    }

    public async Task<DatoField> UpdateFieldAsync(string apiToken, string fieldId, DatoFieldAttributes attributes, CancellationToken cancellationToken = default)
    {
        var body = new { data = new { type = "field", id = fieldId, attributes } };
        var resp = await SendAsync(BuildRequest(HttpMethod.Put, $"{_config.DatoApiBaseUrl}/fields/{fieldId}", apiToken, body), cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<DatoSingleResponse<DatoField>>(json, JsonOptions);
        return result!.Data;
    }

    public async Task DeleteFieldAsync(string apiToken, string fieldId, CancellationToken cancellationToken = default)
    {
        await SendAsync(BuildRequest(HttpMethod.Delete, $"{_config.DatoApiBaseUrl}/fields/{fieldId}", apiToken), cancellationToken);
    }

    public async Task CreateLocaleAsync(string apiToken, string localeCode, CancellationToken cancellationToken = default)
    {
        var body = new { data = new { type = "site_locale", attributes = new { locale = localeCode } } };
        await SendAsync(BuildRequest(HttpMethod.Post, $"{_config.DatoApiBaseUrl}/site/locales", apiToken, body), cancellationToken);
    }

    public async Task DeleteLocaleAsync(string apiToken, string localeId, CancellationToken cancellationToken = default)
    {
        await SendAsync(BuildRequest(HttpMethod.Delete, $"{_config.DatoApiBaseUrl}/site/locales/{localeId}", apiToken), cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("DatoCMS API error {StatusCode}: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
        return response;
    }
}

internal class DatoListResponse<T>
{
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();
}

internal class DatoSingleResponse<T>
{
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public T Data { get; set; } = default!;
}
