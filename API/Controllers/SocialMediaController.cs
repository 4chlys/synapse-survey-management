using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.SocialMedia;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.AiImageGeneration;
using SynapseEnqueteManagement.BL.AiSocialMedia;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/social")]
public class SocialMediaController(
    IAiSocialPostService socialPostService,
    IPlatformAiConfigService platformAiConfig,
    IProjectManager projectManager,
    IParticipantManager participantManager,
    IDashboardManager dashboardManager,
    IAiImageGenerationService imageGenerator,
    IStorageService storage) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateSocialPostResponseDto>> Generate(
        int projectId, GenerateSocialPostRequestDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<SocialPlatform>(dto.Platform, ignoreCase: true, out var platform))
            return BadRequest(new { error = "Invalid platform. Use LinkedIn, Facebook, or Instagram." });

        var project = await projectManager.GetProjectByIdWithIntroductionReadOnlyAsync(projectId);
        if (project is null) return NotFound();

        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var defaultLang = project.DefaultLanguage?.Code ?? lang;

        var completionCount = await participantManager.CountCompletionsByProjectAsync(projectId);
        var metrics         = await dashboardManager.GetDashboardMetricsAsync(projectId, lang, defaultLang);
        var completionRate  = project.ViewCount > 0
            ? Math.Round((double)completionCount / project.ViewCount * 100, 1) : 0d;

        var projectTr = project.Translations.FirstOrDefault(t => t.Language?.Code == lang)
                        ?? project.Translations.FirstOrDefault(t => t.Language?.Code == defaultLang)
                        ?? project.Translations.FirstOrDefault();
        var introTr = project.Introduction?.Translations.FirstOrDefault(t => t.Language?.Code == lang)
                      ?? project.Introduction?.Translations.FirstOrDefault(t => t.Language?.Code == defaultLang)
                      ?? project.Introduction?.Translations.FirstOrDefault();

        var intro = HtmlHelpers.StripHtml(introTr?.Text ?? "");

        var stats = new SocialPostStats(
            project.ViewCount,
            completionCount,
            completionRate,
            metrics.TopIdeas.Take(3).Select(i => i.Description).ToList(),
            metrics.TopCategories.Take(3).Select(c => c.Name).ToList());

        var request = new SocialPostRequest(
            platform,
            projectTr?.Name ?? "",
            projectTr?.Topic ?? "",
            intro,
            project.ProjectPlatform?.Name ?? "",
            dto.UserDescription,
            stats);

        var platformConfig = await platformAiConfig.ResolveAsync(project.ProjectPlatformId, ct);
        var reply = await socialPostService.ChatAsync(request, platformConfig, projectId, ct);

        return Ok(new GenerateSocialPostResponseDto
        {
            Copy              = reply.Copy,
            SuggestedHashtags = reply.SuggestedHashtags.ToList(),
            CharacterCount    = reply.CharacterCount,
            Platform          = platform.ToString(),
        });
    }

    [HttpPost("image")]
    public async Task<ActionResult<GenerateSocialImageResponseDto>> GenerateImage(
        int projectId, CancellationToken ct)
    {
        var project = await projectManager.GetProjectByIdWithIntroductionReadOnlyAsync(projectId);
        if (project is null) return NotFound();

        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var defaultLang = project.DefaultLanguage?.Code ?? lang;
        var projectTr = project.Translations.FirstOrDefault(t => t.Language?.Code == lang)
                        ?? project.Translations.FirstOrDefault(t => t.Language?.Code == defaultLang)
                        ?? project.Translations.FirstOrDefault();
        var introTr = project.Introduction?.Translations.FirstOrDefault(t => t.Language?.Code == lang)
                      ?? project.Introduction?.Translations.FirstOrDefault(t => t.Language?.Code == defaultLang)
                      ?? project.Introduction?.Translations.FirstOrDefault();
        var result = await imageGenerator.GenerateProjectImageAsync(
            projectTr?.Name ?? "", projectId,
            projectTopic: projectTr?.Topic,
            introText: introTr?.Text ?? "",
            ct: ct);
        if (!result.IsSuccess)
            return StatusCode(502, new { error = result.ErrorMessage });

        var ext = result.MimeType == "image/jpeg" ? ".jpg" : ".png";
        var url = await storage.UploadBytesAsync(
            result.ImageBytes, "social", $"{Guid.NewGuid():N}{ext}", result.MimeType);

        return Ok(new GenerateSocialImageResponseDto { ImageUrl = url });
    }
}
