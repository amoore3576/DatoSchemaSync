using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DatoSchemaSync.Models;

namespace DatoSchemaSync.Services;

public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultService> _logger;

    public KeyVaultService(ILogger<KeyVaultService> logger, IOptions<SyncConfiguration> config)
    {
        _logger = logger;
        _secretClient = new SecretClient(new Uri(config.Value.KeyVaultUri), new DefaultAzureCredential());
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving secret '{SecretName}' from Key Vault", secretName);
        var secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        return secret.Value.Value;
    }
}
