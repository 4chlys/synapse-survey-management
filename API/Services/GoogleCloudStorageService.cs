using Google;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.StaticFiles;
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
        using var stream = file.OpenReadStream();
        return await UploadStreamAsync($"{folder}/{Guid.NewGuid()}/{file.FileName}", file.ContentType, stream);
    }

    public async Task<string> UploadBytesAsync(byte[] data, string folder, string fileName, string contentType)
    {
        using var stream = new MemoryStream(data);
        return await UploadStreamAsync($"{folder}/{Guid.NewGuid()}/{fileName}", contentType, stream);
    }

    // Shared upload primitive: stores the stream at objectName and returns its public URL.
    private async Task<string> UploadStreamAsync(string objectName, string contentType, Stream stream)
    {
        var storage = await StorageClient.CreateAsync();
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
        // Never delete app-bundled seed files: they are shared static content needed to re-seed
        // the database on restart. Removing the entity's link is fine; the file must stay.
        if (SeedAsset.IsSeedUrl(url))
            return;

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

    public async Task<string> PublishSeedMediaAsync(string seedSourceDirectory)
    {
        // Seed media lives in GCS alongside user uploads so browsers never hit the API host
        // (which is internal-only). Deterministic "seed/{relativePath}" objects so the stored URLs stay stable.
        var baseUrl = $"https://storage.googleapis.com/{_bucketName}/seed";
        if (!Directory.Exists(seedSourceDirectory))
        {
            logger.LogWarning("[Storage] Seed source directory {Dir} not found; skipping seed media upload", seedSourceDirectory);
            return baseUrl;
        }

        var contentTypes = new FileExtensionContentTypeProvider();
        foreach (var path in Directory.EnumerateFiles(seedSourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(seedSourceDirectory, path).Replace('\\', '/');
            if (!contentTypes.TryGetContentType(path, out var contentType))
                contentType = "application/octet-stream";

            await using var stream = File.OpenRead(path);
            await UploadStreamAsync($"seed/{relativePath}", contentType, stream);
        }

        return baseUrl;
    }
}
