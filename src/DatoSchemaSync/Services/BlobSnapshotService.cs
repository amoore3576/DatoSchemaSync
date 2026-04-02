using System.Text.Json;
using Azure.Storage.Blobs;
using DatoSchemaSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatoSchemaSync.Services;

public class BlobSnapshotService : ISnapshotService
{
    private readonly ILogger<BlobSnapshotService> _logger;
    private readonly SyncConfiguration _config;

    public BlobSnapshotService(ILogger<BlobSnapshotService> logger, IOptions<SyncConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    private BlobClient GetBlobClient()
    {
        var container = new BlobContainerClient(_config.BlobConnectionString, _config.BlobContainerName);
        return container.GetBlobClient(_config.SnapshotBlobName);
    }

    public async Task<DatoSchema?> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = GetBlobClient();
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
        var blobClient = GetBlobClient();
        var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        _logger.LogInformation("Snapshot saved to blob storage.");
    }
}
