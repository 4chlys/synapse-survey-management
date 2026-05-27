using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.UI.User.Options;

namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

public class IdeaImageUrlValidator(
    IConfiguration configuration,
    IWebHostEnvironment env,
    IOptions<StorageOptions> storageOptions)
{
    public bool IsAllowed(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5159";
        if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
            && (uri.Scheme == "https" || uri.Scheme == "http")
            && string.Equals(uri.Authority, apiUri.Authority, StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.StartsWith("/uploads/tmp/ideas/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (env.IsDevelopment()) return false;

        var bucket = storageOptions.Value.BucketName;
        return uri.Scheme == "https"
            && string.Equals(uri.Host, "storage.googleapis.com", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.StartsWith($"/{bucket}/tmp/ideas/", StringComparison.OrdinalIgnoreCase);
    }
}
