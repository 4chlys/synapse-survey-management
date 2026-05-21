using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Upload;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize]
[Route("api/projects/{projectId:int}/social")]
public class SocialMediaProxyController(
    SocialMediaProxyService proxy,
    UploadAdminService uploadService,
    ProjectAdminService projectService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private async Task<bool> CanAccessProjectAsync(int projectId)
    {
        if (User.IsInRole("SystemAdmin")) return true;
        var user = await userManager.GetUserAsync(User);
        if (user?.ProjectPlatformId is null) return false;
        var project = await projectService.GetProjectByIdAsync(projectId);
        return project?.PlatformId == user.ProjectPlatformId;
    }

    [HttpPost("generate"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> Generate(int projectId)
    {
        if (!await CanAccessProjectAsync(projectId)) return Forbid();
        var body = new StreamContent(Request.Body);
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var (status, content) = await proxy.PostAsync($"api/projects/{projectId}/social/generate", body);
        return new ContentResult { StatusCode = status, Content = content, ContentType = "application/json" };
    }

    [HttpPost("image"), IgnoreAntiforgeryToken, EnableRateLimiting("ai-image-generation")]
    public async Task<IActionResult> GenerateImage(int projectId)
    {
        if (!await CanAccessProjectAsync(projectId)) return Forbid();
        var (status, content) = await proxy.PostAsync($"api/projects/{projectId}/social/image");
        return new ContentResult { StatusCode = status, Content = content, ContentType = "application/json" };
    }

    [HttpPost("upload-image"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(int projectId, IFormFile file)
    {
        if (!await CanAccessProjectAsync(projectId)) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new { error = "No file" });
        var url = await uploadService.UploadImageAsync(file);
        return Ok(new { url });
    }
}
