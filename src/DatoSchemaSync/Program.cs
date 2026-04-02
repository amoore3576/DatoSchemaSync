using DatoSchemaSync.Models;
using DatoSchemaSync.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

// Configuration
services.Configure<SyncConfiguration>(configuration);

// Logging
services.AddLogging(logging =>
{
    logging.AddConsole();

    var appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        logging.AddApplicationInsights(
            configureTelemetryConfiguration: config =>
                config.ConnectionString = appInsightsConnectionString,
            configureApplicationInsightsLoggerOptions: _ => { });
    }
});

// HTTP client
services.AddHttpClient<IDatoApiClient, DatoApiClient>();

// Azure services
var keyVaultUri = configuration["KeyVaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    services.AddSingleton<IKeyVaultService, KeyVaultService>();
}

services.AddSingleton<ISnapshotService, BlobSnapshotService>();
services.AddSingleton<IMappingService, BlobMappingService>();

// Core services
services.AddSingleton<ISchemaDiffService, SchemaDiffService>();
services.AddSingleton<ISchemaApplier, SchemaApplier>();
services.AddSingleton<SyncOrchestrator>();

var serviceProvider = services.BuildServiceProvider();

// Optionally load secrets from Key Vault and override configuration
var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SyncConfiguration>>().Value;
if (!string.IsNullOrEmpty(config.KeyVaultUri))
{
    var keyVault = serviceProvider.GetRequiredService<IKeyVaultService>();
    config.SourceApiToken = await keyVault.GetSecretAsync(config.SourceApiTokenSecretName);
    config.DestinationApiToken = await keyVault.GetSecretAsync(config.DestinationApiTokenSecretName);
    if (string.IsNullOrEmpty(config.BlobConnectionString))
        config.BlobConnectionString = await keyVault.GetSecretAsync(config.BlobConnectionStringSecretName);
}

// Run the orchestrator
var orchestrator = serviceProvider.GetRequiredService<SyncOrchestrator>();
await orchestrator.RunAsync();
