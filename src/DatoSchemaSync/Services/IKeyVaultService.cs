namespace DatoSchemaSync.Services;

public interface IKeyVaultService
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
}
