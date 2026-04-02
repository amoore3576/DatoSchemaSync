using System.Text.Json;
using Azure.Storage.Blobs;
using DatoSchemaSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatoSchemaSync.Services;

public class BlobMappingService : IMappingService
{
    private readonly ILogger<BlobMappingService> _logger;
    private readonly SyncConfiguration _config;

    public BlobMappingService(ILogger<BlobMappingService> logger, IOptions<SyncConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    private BlobClient GetBlobClient()
    {
        var container = new BlobContainerClient(_config.BlobConnectionString, _config.BlobContainerName);
        return container.GetBlobClient(_config.MappingBlobName);
    }

    public async Task<IdMapping> LoadMappingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = GetBlobClient();
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogInformation("No existing mapping found. Starting fresh.");
                return new IdMapping();
            }
            var download = await blobClient.DownloadContentAsync(cancellationToken);
            var json = download.Value.Content.ToString();
            return JsonSerializer.Deserialize<IdMapping>(json) ?? new IdMapping();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mapping from blob storage");
            return new IdMapping();
        }
    }

    public async Task SaveMappingAsync(IdMapping mapping, CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient();
        var json = JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true });
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        _logger.LogInformation("ID mapping saved to blob storage.");
    }
}
