namespace DatoSchemaSync.Models;

public class SyncConfiguration
{
    public string SourceApiToken { get; set; } = string.Empty;
    public string DestinationApiToken { get; set; } = string.Empty;
    public string BlobConnectionString { get; set; } = string.Empty;
    public string BlobContainerName { get; set; } = "datoschemasync";
    public string SnapshotBlobName { get; set; } = "schema-snapshot.json";
    public string MappingBlobName { get; set; } = "id-mapping.json";
    public string KeyVaultUri { get; set; } = string.Empty;
    public string SourceApiTokenSecretName { get; set; } = "dato-source-api-token";
    public string DestinationApiTokenSecretName { get; set; } = "dato-destination-api-token";
    public string BlobConnectionStringSecretName { get; set; } = "blob-connection-string";
    public bool DryRun { get; set; } = false;
    public string DatoApiBaseUrl { get; set; } = "https://site-api.datocms.com";
}
