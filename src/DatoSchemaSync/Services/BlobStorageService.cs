using System.Text.Json;
using Azure.Storage.Blobs;
using DatoSchemaSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatoSchemaSync.Services;

public class BlobStorageService : IStorageService
{
    private readonly ILogger<BlobStorageService> _logger;
    private readonly SyncConfiguration _config;

    public BlobStorageService(ILogger<BlobStorageService> logger, IOptions<SyncConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    private BlobClient GetBlobClient(string blobName)
    {
        var container = new BlobContainerClient(_config.BlobConnectionString, _config.BlobContainerName);
        return container.GetBlobClient(blobName);
    }

    public async Task<DatoSchema?> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = GetBlobClient(_config.SnapshotBlobName);
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogInformation("No existing snapshot found. First run.");
                return null;
            }
            var download = await blobClient.DownloadContentAsync(cancellationToken);
            var json = download.Value.Content.ToString();
            return JsonSerializer.Deserialize<DatoSchema>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load snapshot from blob storage");
            return null;
        }
    }

    public async Task SaveSnapshotAsync(DatoSchema schema, CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient(_config.SnapshotBlobName);
        var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        _logger.LogInformation("Snapshot saved to blob storage.");
    }

    public async Task<IdMapping> LoadMappingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = GetBlobClient(_config.MappingBlobName);
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
        var blobClient = GetBlobClient(_config.MappingBlobName);
        var json = JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true });
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        _logger.LogInformation("ID mapping saved to blob storage.");
    }
}
