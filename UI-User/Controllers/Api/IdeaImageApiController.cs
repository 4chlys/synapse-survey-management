using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SynapseEnqueteManagement.UI.User.Services.IdeaImage;

namespace SynapseEnqueteManagement.UI.User.Controllers.Api;

[ApiController]
[Route("{projectId:int}/api/[controller]")]
public class IdeaImageApiController(IIdeaImageService ideaImageService) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif"
    ];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [EnableRateLimiting("ai-image-moderation")]
    public async Task<IActionResult> Upload(int projectId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { reason = "Geen bestand ontvangen." });

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { reason = "Alleen afbeeldingen (jpeg, png, webp, gif) zijn toegestaan." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { reason = "De afbeelding mag maximaal 10 MB zijn." });

        var result = await ideaImageService.UploadAsync(file, projectId, ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { reason = result.Reason });

        return Ok(new
        {
            url = result.Url,
            imageModerationToken = result.ImageModerationToken,
            isAcceptable = result.IsAcceptable,
            reason = result.Reason
        });
    }

    [HttpPost("describe")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [EnableRateLimiting("ai-image-describe")]
    public async Task<IActionResult> Describe(
        int projectId,
        IFormFile file, [FromQuery] string categoryName, [FromQuery] string categoryDescription,
        [FromQuery] string language, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { reason = "Geen bestand ontvangen." });

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { reason = "Alleen afbeeldingen (jpeg, png, webp, gif) zijn toegestaan." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { reason = "De afbeelding mag maximaal 10 MB zijn." });

        var result = await ideaImageService.DescribeAsync(file, projectId, categoryName ?? "", categoryDescription ?? "", language, ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { reason = result.Reason });

        return Ok(new { description = result.Description });
    }
}
