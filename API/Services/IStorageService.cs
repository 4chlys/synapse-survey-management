namespace SynapseEnqueteManagement.API.Services;

public interface IStorageService
{
    Task<string> UploadAsync(IFormFile file, string folder);
    Task<string> UploadBytesAsync(byte[] data, string folder, string fileName, string contentType);
    /// <summary>
    /// Moves a file from the tmp/ prefix to its permanent location.
    /// Returns the new permanent URL.
    /// </summary>
    Task<string> PromoteAsync(string tmpUrl);
    Task DeleteAsync(string url);
    /// <summary>
    /// Publishes the app-bundled seed media (under <paramref name="seedSourceDirectory"/>) so
    /// browsers can load it, and returns the public base URL for seed assets (no trailing slash),
    /// e.g. "https://storage.googleapis.com/{bucket}/seed". Cloud storage uploads the bundled files;
    /// local storage serves them from wwwroot and only returns the base URL. Returns null when no
    /// base URL is configured.
    /// </summary>
    Task<string> PublishSeedMediaAsync(string seedSourceDirectory);
}
