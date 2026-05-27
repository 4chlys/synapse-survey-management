using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.API.Options;

namespace SynapseEnqueteManagement.API.Services;

public class GoogleCloudStorageService(
    ILogger<GoogleCloudStorageService> logger,
    IOptions<StorageOptions> storageOptions) : IStorageService
{
    private readonly string _bucketName = storageOptions.Value.BucketName;

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var storage = await StorageClient.CreateAsync();
        var objectName = $"{folder}/{Guid.NewGuid()}/{file.FileName}";

        using var stream = file.OpenReadStream();
        await storage.UploadObjectAsync(_bucketName, objectName, file.ContentType, stream);

        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }

    public async Task<string> UploadBytesAsync(byte[] data, string folder, string fileName, string contentType)
    {
        var storage = await StorageClient.CreateAsync();
        var objectName = $"{folder}/{Guid.NewGuid()}/{fileName}";

        using var stream = new MemoryStream(data);
        await storage.UploadObjectAsync(_bucketName, objectName, contentType, stream);

        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }

    public async Task<string> PromoteAsync(string tmpUrl)
    {
        // GCS URL shape: https://storage.googleapis.com/{bucket}/tmp/{rest}
        // Promoted URL:  https://storage.googleapis.com/{bucket}/{rest}
        var prefix = $"https://storage.googleapis.com/{_bucketName}/tmp/";
        if (!tmpUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return tmpUrl; // already permanent

        var rest = tmpUrl[prefix.Length..]; // "ideas/{guid}/file.jpg"
        var srcObject = $"tmp/{rest}";
        var dstObject = rest;

        var storage = await StorageClient.CreateAsync();
        await storage.CopyObjectAsync(_bucketName, srcObject, _bucketName, dstObject);
        await storage.DeleteObjectAsync(_bucketName, srcObject);

        return $"https://storage.googleapis.com/{_bucketName}/{dstObject}";
    }

    public async Task DeleteAsync(string url)
    {
        try
        {
            // GCS URL shape: https://storage.googleapis.com/{bucket}/{objectName}
            var prefix = $"https://storage.googleapis.com/{_bucketName}/";
            if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("[Storage] URL {Url} does not match bucket {Bucket}, skipping delete", url, _bucketName);
                return;
            }
            var objectName = url[prefix.Length..];
            var storage = await StorageClient.CreateAsync();
            await storage.DeleteObjectAsync(_bucketName, objectName);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("[Storage] GCS object not found for URL {Url}, nothing to delete", url);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Storage] Failed to delete GCS object for URL {Url}", url);
        }
    }
}
