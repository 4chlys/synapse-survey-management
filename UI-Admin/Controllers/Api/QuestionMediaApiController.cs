using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

namespace SynapseEnqueteManagement.UI.Admin.Controllers.Api;

[ApiController]
[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
[Route("api/admin/projects/{projectId:int}/questions/media")]
public class QuestionMediaApiController(IQuestionMediaService mediaService) : ControllerBase
{
    private static readonly HashSet<string> AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private static readonly HashSet<string> AllowedVideoTypes = ["video/mp4", "video/webm"];
    private static readonly HashSet<string> AllowedAudioTypes = ["audio/mpeg", "audio/mp4", "audio/wav", "audio/ogg", "audio/x-m4a"];

    [HttpPost("upload")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
    [EnableRateLimiting("question-media-upload")]
    public async Task<IActionResult> Upload(int projectId, IFormFile file,
        [FromQuery] QuestionMediaType mediaType, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        var (allowed, maxBytes) = mediaType switch
        {
            QuestionMediaType.Image => (AllowedImageTypes, 5L * 1024 * 1024),
            QuestionMediaType.Video => (AllowedVideoTypes, 25L * 1024 * 1024),
            QuestionMediaType.Audio => (AllowedAudioTypes, 20L * 1024 * 1024),
            _ => (null, 0L)
        };

        if (allowed is null)
            return BadRequest("Invalid media type.");
        if (!allowed.Contains(file.ContentType))
            return BadRequest($"File type '{file.ContentType}' is not allowed for {mediaType}.");
        if (file.Length > maxBytes)
            return BadRequest($"File exceeds the {maxBytes / (1024 * 1024)} MB limit for {mediaType}.");

        var result = await mediaService.UploadAsync(file, mediaType, ct);
        if (!result.Success)
            return BadRequest(result.Error);

        return Ok(new { url = result.Url });
    }
}
