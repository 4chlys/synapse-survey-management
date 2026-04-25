using Google.Cloud.Storage.V1;

namespace SynapseEnqueteManagement.API.Services;

public class GoogleCloudStorageService : IStorageService
{
    private readonly string _bucketName;

    public GoogleCloudStorageService()
    {
        _bucketName = Environment.GetEnvironmentVariable("GCS_BUCKET") ?? "synapse-assets-prod";
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var storage = await StorageClient.CreateAsync();
        var objectName = $"{folder}/{Guid.NewGuid()}/{file.FileName}";

        using var stream = file.OpenReadStream();
        await storage.UploadObjectAsync(_bucketName, objectName, file.ContentType, stream);

        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }
}
