using DatoSchemaSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatoSchemaSync.Services;

public class SyncOrchestrator
{
    private readonly IDatoApiClient _apiClient;
    private readonly ISnapshotService _snapshotService;
    private readonly IMappingService _mappingService;
    private readonly ISchemaDiffService _diffService;
    private readonly ISchemaApplier _schemaApplier;
    private readonly ILogger<SyncOrchestrator> _logger;
    private readonly SyncConfiguration _config;

    public SyncOrchestrator(
        IDatoApiClient apiClient,
        ISnapshotService snapshotService,
        IMappingService mappingService,
        ISchemaDiffService diffService,
        ISchemaApplier schemaApplier,
        ILogger<SyncOrchestrator> logger,
        IOptions<SyncConfiguration> config)
    {
        _apiClient = apiClient;
        _snapshotService = snapshotService;
        _mappingService = mappingService;
        _diffService = diffService;
        _schemaApplier = schemaApplier;
        _logger = logger;
        _config = config.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting DatoSchemaSync. DryRun={DryRun}", _config.DryRun);

        // 1. Fetch current schema from source
        var currentSchema = await _apiClient.FetchSchemaAsync(_config.SourceApiToken, cancellationToken);

        // 2. Load previous snapshot
        var previousSnapshot = await _snapshotService.LoadSnapshotAsync(cancellationToken);

        // 3. If no previous snapshot, treat everything as new
        if (previousSnapshot == null)
        {
            _logger.LogInformation("No previous snapshot found. Treating all schema objects as new.");
            previousSnapshot = new DatoSchema();
        }

        // 4. Compute diff
        var diff = _diffService.ComputeDiff(previousSnapshot, currentSchema);

        if (!diff.HasChanges)
        {
            _logger.LogInformation("Schema is up to date. No changes needed.");
        }
        else
        {
            // 5. Load ID mapping
            var mapping = await _mappingService.LoadMappingAsync(cancellationToken);

            // 6. Apply changes
            await _schemaApplier.ApplyDiffAsync(diff, currentSchema, mapping, cancellationToken);

            // 7. Save updated mapping
            if (!_config.DryRun)
                await _mappingService.SaveMappingAsync(mapping, cancellationToken);
        }

        // 8. Save current snapshot (even on dry run, so next run can diff properly)
        if (!_config.DryRun)
            await _snapshotService.SaveSnapshotAsync(currentSchema, cancellationToken);

        _logger.LogInformation("DatoSchemaSync completed successfully.");
    }
}
