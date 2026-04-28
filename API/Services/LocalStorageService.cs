namespace SynapseEnqueteManagement.API.Services;

public class LocalStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : IStorageService
{
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        var request = httpContextAccessor.HttpContext!.Request;
        return $"{request.Scheme}://{request.Host}/uploads/{folder}/{fileName}";
    }
}
