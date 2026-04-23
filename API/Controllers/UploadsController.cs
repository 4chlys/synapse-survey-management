using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Services;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadsController(IStorageService storageService) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif"
    ];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest("Only image files (jpeg, png, webp, gif) are allowed.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File exceeds the 5 MB limit.");

        var url = await storageService.UploadAsync(file, "logos");
        return Ok(new UploadResultDto { Url = url });
    }
}

public class UploadResultDto
{
    public string Url { get; set; }
}
