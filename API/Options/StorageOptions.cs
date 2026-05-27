namespace SynapseEnqueteManagement.API.Options;

public sealed class StorageOptions
{
    // GCS bucket backing IStorageService and question-media URL validation.
    // Bound once at startup from the GCS_BUCKET environment variable.
    public string BucketName { get; set; } = "synapse-assets-prod";
}
