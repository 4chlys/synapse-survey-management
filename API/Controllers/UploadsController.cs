using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadsController(
    IStorageService storageService,
    IAiModerationService aiModerationService,
    IModerationTokenService moderationTokenService) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif"
    ];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest("Only image files (jpeg, png, webp, gif) are allowed.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File exceeds the 10 MB limit.");

        var url = await storageService.UploadAsync(file, "logos");
        return Ok(new UploadResultDto { Url = url });
    }

    [HttpPost("idea-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<IdeaImageUploadResultDto>> UploadIdeaImage(
        IFormFile file, [FromQuery] int projectId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest("Only image files (jpeg, png, webp, gif) are allowed.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File exceeds the 10 MB limit.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var modResult = await aiModerationService.CheckImageWithAiAsync(bytes, file.ContentType, ct);
        var imageStatus = modResult.IsAcceptable ? ModerationStatus.Approved : ModerationStatus.Flagged;

        // Always upload — token encodes the AI result so the idea can be stored as Flagged when rejected.
        // Images land in tmp/ideas/ so orphaned uploads are cleaned up by GCS lifecycle rules.
        var tmpUrl = await storageService.UploadAsync(file, "tmp/ideas");
        var imageModerationToken = moderationTokenService.GenerateImageToken(bytes, projectId, imageStatus);

        return Ok(new IdeaImageUploadResultDto
        {
            Url = tmpUrl,
            ImageModerationToken = imageModerationToken,
            IsAcceptable = modResult.IsAcceptable,
            Reason = modResult.IsAcceptable ? null : modResult.Reason
        });
    }
}

public class UploadResultDto
{
    public string Url { get; set; }
}

public class IdeaImageUploadResultDto
{
    public string Url { get; set; }
    public string ImageModerationToken { get; set; }
    public bool IsAcceptable { get; set; }
    public string Reason { get; set; }
}
