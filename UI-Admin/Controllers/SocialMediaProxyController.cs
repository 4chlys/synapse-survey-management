using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Upload;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
[Route("api/projects/{projectId:int}/social")]
public class SocialMediaProxyController(
    SocialMediaProxyService proxy,
    UploadAdminService uploadService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpPost("generate"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> Generate(int projectId)
    {
        var body = new StreamContent(Request.Body);
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var (status, content) = await proxy.PostAsync($"api/projects/{projectId}/social/generate", body);
        return new ContentResult { StatusCode = status, Content = content, ContentType = "application/json" };
    }

    [HttpPost("image"), IgnoreAntiforgeryToken, EnableRateLimiting("ai-image-generation")]
    public async Task<IActionResult> GenerateImage(int projectId)
    {
        var body = new StreamContent(Request.Body);
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var (status, content) = await proxy.PostAsync($"api/projects/{projectId}/social/image", body);
        return new ContentResult { StatusCode = status, Content = content, ContentType = "application/json" };
    }

    [HttpPost("upload-image"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(int projectId, IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = localizer["Upload_NoFileProvided"].Value });
        var url = await uploadService.UploadImageAsync(file);
        return Ok(new { url });
    }
}
