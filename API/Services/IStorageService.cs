namespace SynapseEnqueteManagement.API.Services;

public interface IStorageService
{
    Task<string> UploadAsync(IFormFile file, string folder);
    /// <summary>
    /// Moves a file from the tmp/ prefix to its permanent location.
    /// Returns the new permanent URL.
    /// </summary>
    Task<string> PromoteAsync(string tmpUrl);
    Task DeleteAsync(string url);
}
