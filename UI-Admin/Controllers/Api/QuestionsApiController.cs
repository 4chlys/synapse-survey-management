using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

namespace SynapseEnqueteManagement.UI.Admin.Controllers.Api;

[ApiController]
[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
[Route("api/admin/projects/{projectId:int}/questions")]
public class QuestionsApiController(
    QuestionAdminService questionService,
    ProjectAdminService projectService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdminQuestionViewModel>>> GetQuestions(int projectId)
    {
        // Returns the multilingual admin shape (Translations[] + per-language
        // EndingAnswers + AnswerChoiceCount) so the edit modal can rehydrate
        // every supported language without re-fetching per language.
        var questions = await questionService.GetQuestionsAsync(projectId);
        return Ok(questions);
    }

    [HttpPut("{questionId:int}/location")]
    public async Task<ActionResult> Move(int projectId, int questionId, MoveRequest request)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project?.ProjectState is "Active" or "Archived")
            return StatusCode(403, localizer["Project_IsLocked"].Value);
        try
        {
            await questionService.MoveQuestionAsync(projectId, questionId,
                request.ParentQuestionId, request.ChoiceIndex, request.Position);
            return NoContent();
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{questionId:int}/ending-answers")]
    public async Task<ActionResult> UpdateEndingAnswers(int projectId, int questionId, EndingAnswersRequest request)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project?.ProjectState is "Active" or "Archived")
            return StatusCode(403, localizer["Project_IsLocked"].Value);
        var questions = await questionService.GetQuestionsAsync(projectId);
        var question = questions.FirstOrDefault(q => q.QuestionId == questionId);
        if (question is null) return NotFound();

        // The tree toggle sends single-language trigger strings (the choice text
        // visible in the rendered tree). Resolve each trigger to a choice-slot
        // index by matching against the default language's AnswerChoices, then
        // build per-language ending-answer slots by copying that slot's values
        // from every supported language. Slot order matches choice-slot order.
        var defaultLang = project?.DefaultLanguageCode;
        var defaultTranslation =
            question.Translations.FirstOrDefault(t => t.LanguageCode == defaultLang)
            ?? question.Translations.FirstOrDefault();
        var defaultChoices = defaultTranslation?.AnswerChoices?.ToList() ?? new List<string>();

        var triggerSet = new HashSet<string>(request.EndingAnswers ?? new List<string>(), StringComparer.Ordinal);
        var selectedSlotIndices = Enumerable.Range(0, defaultChoices.Count)
            .Where(i => triggerSet.Contains(defaultChoices[i]))
            .OrderBy(i => i)
            .ToList();

        var endingSlots = selectedSlotIndices
            .Select(slotIdx => new EndingAnswerSlotFormViewModel
            {
                Translations = question.Translations
                    .Select(t => new EndingAnswerTranslationFormViewModel
                    {
                        LanguageCode = t.LanguageCode,
                        Value = slotIdx < (t.AnswerChoices?.Count ?? 0)
                            ? t.AnswerChoices.ElementAt(slotIdx)
                            : string.Empty,
                    })
                    .ToList()
            })
            .ToList();

        // Re-submit the question with its existing translations untouched and
        // the freshly-rebuilt ending-answer slots. We must pass translations
        // explicitly (the manager upserts them); passing null endings on this
        // path would preserve old ones rather than apply the toggle.
        var translations = question.Translations
            .Select(t => new QuestionTranslationFormViewModel
            {
                LanguageCode = t.LanguageCode,
                Text = t.Text,
                MediaText = t.MediaText,
                AnswerChoicesRaw = string.Join('\n', t.AnswerChoices ?? new List<string>()),
            })
            .ToList();

        // Non-null collection forces an explicit endings write (empty = clear all);
        // the service distinguishes null (preserve) from empty (clear).
        await questionService.UpdateQuestionAsync(projectId, questionId,
            question.QuestionFormat, question.IsMandatory, question.AnswerChoiceCount,
            translations, question.SectionId,
            endingAnswerSlots: endingSlots,
            mediaUrl: question.MediaUrl, mediaType: question.MediaType,
            mediaIsEmbed: question.MediaIsEmbed);
        return NoContent();
    }

    public record MoveRequest(int? ParentQuestionId, int? ChoiceIndex, int Position);

    public record EndingAnswersRequest
    {
        public ICollection<string> EndingAnswers { get; init; } = new List<string>();
    }
}
