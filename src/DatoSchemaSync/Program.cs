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

services.AddSingleton<IStorageService, BlobStorageService>();

// Core services
services.AddSingleton<ISchemaDiffService, SchemaDiffService>();
services.AddSingleton<ISchemaApplier, SchemaApplier>();
services.AddSingleton<SyncOrchestrator>();

var serviceProvider = services.BuildServiceProvider();

// Load secrets from Key Vault if configured
await serviceProvider.LoadSecretsFromKeyVaultAsync();

// Run the orchestrator
var orchestrator = serviceProvider.GetRequiredService<SyncOrchestrator>();
await orchestrator.RunAsync();
