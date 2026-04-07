using DatoSchemaSync.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DatoSchemaSync.Services;

public static class ConfigurationExtensions
{
    public static async Task LoadSecretsFromKeyVaultAsync(this IServiceProvider serviceProvider)
    {
        var config = serviceProvider.GetRequiredService<IOptions<SyncConfiguration>>().Value;

        if (string.IsNullOrEmpty(config.KeyVaultUri))
        {
            return;
        }

        var keyVault = serviceProvider.GetRequiredService<IKeyVaultService>();

        config.SourceApiToken = await keyVault.GetSecretAsync(config.SourceApiTokenSecretName);
        config.DestinationApiToken = await keyVault.GetSecretAsync(config.DestinationApiTokenSecretName);

        if (string.IsNullOrEmpty(config.BlobConnectionString))
        {
            config.BlobConnectionString = await keyVault.GetSecretAsync(config.BlobConnectionStringSecretName);
        }
    }
}
