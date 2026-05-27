namespace SynapseEnqueteManagement.API.Services;

/// <summary>
/// Seed media is app-bundled static content served from <c>wwwroot/seed/…</c> (see
/// <c>DummyDataSeeder</c>). The files are shared by every seeded entity and must survive a
/// restart so the database can be re-seeded. Deleting a seeded entity may remove its row, but the
/// underlying seed file must never be deleted from disk/storage.
/// </summary>
public static class SeedAsset
{
    // Seed URLs have the shape "{base}/seed/{relativePath}". Match the "/seed/" path segment so the
    // check is independent of scheme/host and of which storage backend produced the URL.
    private const string SeedPathSegment = "/seed/";

    public static bool IsSeedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Contains(SeedPathSegment, StringComparison.OrdinalIgnoreCase);

        return uri.AbsolutePath.StartsWith(SeedPathSegment, StringComparison.OrdinalIgnoreCase);
    }
}
