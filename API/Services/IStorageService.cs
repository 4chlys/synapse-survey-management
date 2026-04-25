namespace SynapseEnqueteManagement.API.Services;

public interface IStorageService
{
    Task<string> UploadAsync(IFormFile file, string folder);
}
