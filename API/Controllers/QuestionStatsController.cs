using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questions")]
public class QuestionStatsController(
    IQuestionStatsManager questionStatsManager,
    IProjectManager projectManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ICollection<QuestionLookupDto>>> GetQuestions(
        int projectId,
        [FromQuery] QuestionFormat? format = null,
        [FromQuery] string languageCode = null)
    {
        if (format is null)
            return BadRequest("The 'format' query parameter is required.");

        var project = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (project is null)
            return NotFound();

        var resolvedLanguage = ResolveLanguageFromContext(languageCode);
        var items = await questionStatsManager.ReadQuestionsByFormatAsync(projectId, format.Value, resolvedLanguage);
        return Ok(items.Select(q => new QuestionLookupDto
        {
            QuestionId = q.QuestionId,
            Text = q.Text
        }).ToList());
    }

    [HttpGet("{questionId}/single-stats")]
    public async Task<ActionResult<SingleChoiceStatsDto>> GetSingleChoiceStats(int projectId, int questionId,
        [FromQuery] string languageCode = null)
    {
        var resolvedLanguage = ResolveLanguageFromContext(languageCode);
        var stats = await questionStatsManager.ReadSingleChoiceStatsAsync(projectId, questionId, resolvedLanguage);
        return Ok(new SingleChoiceStatsDto
        {
            QuestionId = stats.QuestionId,
            TotalResponses = stats.TotalResponses,
            Counts = stats.Counts.Select(c => new OptionCountDto
            {
                Option = c.Option,
                Count = c.Count
            }).ToList()
        });
    }

    [HttpGet("{questionId}/multi-stats")]
    public async Task<ActionResult<MultipleChoiceStatsDto>> GetMultipleChoiceStats(int projectId, int questionId,
        [FromQuery] string languageCode = null)
    {
        var resolvedLanguage = ResolveLanguageFromContext(languageCode);
        var stats = await questionStatsManager.ReadMultipleChoiceStatsAsync(projectId, questionId, resolvedLanguage);
        return Ok(new MultipleChoiceStatsDto
        {
            QuestionId = stats.QuestionId,
            TotalResponses = stats.TotalResponses,
            Counts = stats.Counts.Select(c => new OptionCountDto
            {
                Option = c.Option,
                Count = c.Count
            }).ToList()
        });
    }

    [HttpGet("{questionId}/likert-stats")]
    public async Task<ActionResult<LikertStatsDto>> GetLikertStats(int projectId, int questionId)
    {
        var stats = await questionStatsManager.ReadLikertStatsAsync(projectId, questionId);
        return Ok(new LikertStatsDto
        {
            QuestionId = stats.QuestionId,
            TotalResponses = stats.TotalResponses,
            Distribution = stats.Distribution.Select(d => new LikertPointCountDto
            {
                Point = d.Point,
                Count = d.Count
            }).ToList()
        });
    }

    [HttpGet("{questionId}/open-answers")]
    public async Task<ActionResult<OpenAnswerPageDto>> GetOpenAnswers(
        int projectId,
        int questionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var page = await questionStatsManager.ReadOpenAnswerPageAsync(
            projectId, questionId, Math.Max(skip, 0), Math.Clamp(take, 1, 200));
        return Ok(new OpenAnswerPageDto
        {
            QuestionId = page.QuestionId,
            TotalAnswers = page.TotalAnswers,
            Answers = page.Answers.Select(a => new OpenAnswerDto
            {
                Value = a.Value,
                CreatedAt = a.CreatedAt
            }).ToList()
        });
    }

    // BL takes the resolved language as a parameter; the API picks it from request context here.
    // CurrentUICulture is set per request by the localization middleware from the admin's cookie/header,
    // which is why the BL no longer reads it directly.
    private static string ResolveLanguageFromContext(string explicitCode)
        => explicitCode ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
}
