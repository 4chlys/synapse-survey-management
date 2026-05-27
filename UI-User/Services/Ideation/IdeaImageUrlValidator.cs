namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

// URL allow-list policy for idea images: API upload origin (dev + Cloud Run test with
// LocalStorage) always allowed; GCS tmp/ideas allowed in production. Kept out of the
// controller so the policy is one named thing.
public class IdeaImageUrlValidator(IConfiguration configuration, IWebHostEnvironment env)
{
    public bool IsAllowed(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        // Always allow uploads from the configured API origin (local dev + Cloud Run test with LocalStorage)
        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5159";
        if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
            && (uri.Scheme == "https" || uri.Scheme == "http")
            && string.Equals(uri.Authority, apiUri.Authority, StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.StartsWith("/uploads/tmp/ideas/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (env.IsDevelopment()) return false;

        // In production, also allow GCS uploads under tmp/ideas/{bucket}
        var bucket = configuration["GcsBucket"]
                     ?? Environment.GetEnvironmentVariable("GCS_BUCKET")
                     ?? "synapse-assets-prod";
        return uri.Scheme == "https"
            && string.Equals(uri.Host, "storage.googleapis.com", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.StartsWith($"/{bucket}/tmp/ideas/", StringComparison.OrdinalIgnoreCase);
    }
}
