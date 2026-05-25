using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.API.Helpers;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questionnaire")]
public class QuestionnairesController(
    IQuestionnaireManager questionnaireManager,
    ISurveyFlowManager surveyFlowManager,
    IParticipantManager participantManager,
    IProjectManager projectManager,
    IStorageService storageService,
    IWebHostEnvironment env)
    : ParticipantControllerBase(participantManager, projectManager)
{
    private static readonly string GcsBucket =
        Environment.GetEnvironmentVariable("GCS_BUCKET") ?? "synapse-assets-prod";

    [HttpGet]
    public async Task<ActionResult<QuestionnaireDto>> GetQuestionnaire(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        var dto = MapToQuestionnaireDto(questionnaire);
        return Ok(dto);
    }

    [HttpGet("questions")]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions(int projectId,
        [FromQuery] string languageCode = null)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdReadOnlyAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        if (languageCode is null)
        {
            var project = await GetProjectByIdReadOnlyAsync(projectId);
            languageCode = project?.DefaultLanguage?.Code ?? "nl";
        }

        var questions = await questionnaireManager.GetQuestionsForProjectAsync(projectId);
        var dtos = questions.Select(q => MapToDto(q, languageCode)).ToList();
        return Ok(dtos);
    }

    [HttpPost("questions")]
    public async Task<ActionResult<AdminQuestionDto>> AddQuestion(int projectId,
        CreateQuestionDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<QuestionFormat>(dto.QuestionFormat, out var format))
            return BadRequest("Invalid QuestionFormat.");

        var (mediaValid, canonicalUrl, mediaError) = MediaUrlValidator.Validate(
            dto.MediaUrl, dto.MediaType, dto.MediaIsEmbed, GcsBucket, env.IsDevelopment());
        if (!mediaValid)
            return BadRequest(mediaError);

        var translationInputs = dto.Translations.Select(t =>
            new QuestionTranslationInput(t.LanguageCode, t.Text, t.AnswerChoices, t.MediaText));

        var endingAnswerInputs = dto.EndingAnswers?.Select(group =>
            group.Select(t => new EndingAnswerTranslationInput(t.LanguageCode, t.Value)));

        var question = await questionnaireManager.AddQuestionAsync(
            projectId, dto.AnswerChoiceCount, format, dto.IsMandatory,
            translationInputs,
            dto.SectionId,
            dto.ParentQuestionId, dto.ChoiceIndex,
            endingAnswerInputs,
            canonicalUrl, dto.MediaType, dto.MediaIsEmbed,
            ct);

        if (question is null)
            return NotFound();

        return Created(string.Empty, MapToAdminDto(question));
    }

    [HttpPut("questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(int projectId, int questionId, UpdateQuestionDto dto,
        CancellationToken ct)
    {
        if (!Enum.TryParse<QuestionFormat>(dto.QuestionFormat, out var format))
            return BadRequest("Invalid QuestionFormat.");

        if (!await questionnaireManager.QuestionBelongsToProjectAsync(questionId, projectId))
            return NotFound();

        var (mediaValid, canonicalUrl, mediaError) = MediaUrlValidator.Validate(
            dto.MediaUrl, dto.MediaType, dto.MediaIsEmbed, GcsBucket, env.IsDevelopment());
        if (!mediaValid)
            return BadRequest(mediaError);

        var existing = await questionnaireManager.GetQuestionByIdReadOnlyAsync(questionId);
        var oldMediaUrl = existing?.MediaUrl;

        var translationInputs = dto.Translations.Select(t =>
            new QuestionTranslationInput(t.LanguageCode, t.Text, t.AnswerChoices, t.MediaText));

        var endingAnswerInputs = dto.EndingAnswers?.Select(group =>
            group.Select(t => new EndingAnswerTranslationInput(t.LanguageCode, t.Value)));

        var success = await questionnaireManager.UpdateQuestionAsync(
            questionId, dto.AnswerChoiceCount, format, dto.IsMandatory,
            translationInputs,
            dto.SectionId,
            endingAnswerInputs,
            canonicalUrl, dto.MediaType, dto.MediaIsEmbed,
            ct);

        if (!success)
            return NotFound();

        // Orphan cleanup: delete old uploaded file if the URL changed
        if (!string.IsNullOrEmpty(oldMediaUrl) && oldMediaUrl != canonicalUrl
            && existing?.MediaIsEmbed == false)
        {
            try { await storageService.DeleteAsync(oldMediaUrl); }
            catch (Exception ex)
            {
                HttpContext.RequestServices
                    .GetRequiredService<ILogger<QuestionnairesController>>()
                    .LogWarning(ex, "[Media] Failed to delete orphaned question media {Url}", oldMediaUrl);
            }
        }

        return NoContent();
    }

    [HttpDelete("questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(int projectId, int questionId, CancellationToken ct)
    {
        if (!await questionnaireManager.QuestionBelongsToProjectAsync(questionId, projectId))
            return NotFound();

        var existing = await questionnaireManager.GetQuestionByIdReadOnlyAsync(questionId);
        var mediaUrl = existing?.MediaUrl;
        var isEmbed = existing?.MediaIsEmbed ?? false;

        var success = await questionnaireManager.DeleteQuestionAsync(questionId, ct);
        if (!success)
            return NotFound();

        // Orphan cleanup: delete the storage file after the DB row is gone
        if (!string.IsNullOrEmpty(mediaUrl) && !isEmbed)
        {
            try { await storageService.DeleteAsync(mediaUrl); }
            catch (Exception ex)
            {
                HttpContext.RequestServices
                    .GetRequiredService<ILogger<QuestionnairesController>>()
                    .LogWarning(ex, "[Media] Failed to delete question media on question delete {Url}", mediaUrl);
            }
        }

        return NoContent();
    }

    [HttpPut("questions/{questionId}/location")]
    public async Task<ActionResult<AdminQuestionDto>> MoveQuestion(int projectId, int questionId,
        MoveQuestionDto dto, CancellationToken ct)
    {
        if (!await questionnaireManager.QuestionBelongsToProjectAsync(questionId, projectId))
            return NotFound();

        var moved = await questionnaireManager.MoveQuestionAsync(
            questionId, dto.ParentQuestionId, dto.ChoiceIndex, dto.Position, ct);

        if (moved is null)
            return NotFound();

        return Ok(MapToAdminDto(moved));
    }

    [HttpGet("next-question")]
    public async Task<ActionResult> GetNextQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var nextQuestion = await surveyFlowManager.GetNextQuestionAsync(currentQuestionId, participant.ParticipantId);
        return Ok(new { questionId = nextQuestion?.QuestionId });
    }

    [HttpGet("previous-question")]
    public async Task<ActionResult> GetPreviousQuestion(int projectId, [FromQuery] int currentQuestionId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var prevQuestion =
            await surveyFlowManager.GetPreviousQuestionAsync(currentQuestionId, participant.ParticipantId);
        return Ok(new { questionId = prevQuestion?.QuestionId });
    }

    [HttpGet("resume-question")]
    public async Task<ActionResult> GetResumeQuestion(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var resumeQuestion = await surveyFlowManager.GetResumeQuestionAsync(projectId, participant.ParticipantId);
        return Ok(new { questionId = resumeQuestion?.QuestionId });
    }

    [HttpGet("visible-path")]
    public async Task<ActionResult> GetVisiblePath(int projectId)
    {
        var (participant, error) = await GetParticipantAsync(projectId);
        if (error is not null) return error;

        var path = await surveyFlowManager.ComputeVisiblePathAsync(projectId, participant.ParticipantId);
        return Ok(new { questionIds = path });
    }

    private static QuestionnaireDto MapToQuestionnaireDto(Questionnaire questionnaire)
        => new()
        {
            QuestionnaireId = questionnaire.QuestionnaireId,
            Questions = questionnaire.Questions
                .OrderBy(q => q.Position)
                .Select(q => MapToDto(q, null))
                .ToList()
        };

    private static QuestionDto MapToDto(Question question, string languageCode)
    {
        var translation = languageCode is not null
            ? question.Translations?.FirstOrDefault(t => t.Language?.Code == languageCode)
              ?? question.Translations?.FirstOrDefault()
            : question.Translations?.FirstOrDefault();

        return new QuestionDto
        {
            QuestionId = question.QuestionId,
            Text = translation?.Text,
            QuestionFormat = question.QuestionFormat.ToString(),
            AnswerChoices = translation?.AnswerChoices?.ToList() ?? new List<string>(),
            AnswerChoiceCount = question.AnswerChoiceCount,
            IsMandatory = question.IsMandatory,
            Position = question.Position,
            ChoiceIndex = question.ChoiceIndex,
            ParentQuestionId = question.ParentQuestion?.QuestionId,
            EndingAnswers = question.EndingAnswers
                .Select(ea => ea.Translations
                    .FirstOrDefault(t => languageCode is not null && t.Language?.Code == languageCode)?.Value
                    ?? ea.Translations.FirstOrDefault()?.Value)
                .Where(v => v is not null)
                .ToList(),
            SectionName = question.Section?.Translations
                ?.FirstOrDefault(t => languageCode is not null && t.Language?.Code == languageCode)?.Name
                ?? question.Section?.Translations?.FirstOrDefault()?.Name,
            SectionId = question.Section?.SectionId,
            MediaUrl = question.MediaUrl,
            MediaType = question.MediaType,
            MediaIsEmbed = question.MediaIsEmbed,
            MediaText = translation?.MediaText
        };
    }

    private static AdminQuestionDto MapToAdminDto(Question question)
        => new()
        {
            QuestionId = question.QuestionId,
            QuestionFormat = question.QuestionFormat.ToString(),
            IsMandatory = question.IsMandatory,
            Position = question.Position,
            AnswerChoiceCount = question.AnswerChoiceCount,
            ChoiceIndex = question.ChoiceIndex,
            ParentQuestionId = question.ParentQuestion?.QuestionId,
            SectionId = question.Section?.SectionId,
            SectionName = question.Section?.Translations?.FirstOrDefault()?.Name,
            MediaUrl = question.MediaUrl,
            MediaType = question.MediaType,
            MediaIsEmbed = question.MediaIsEmbed,
            Translations = question.Translations?.Select(t => new QuestionTranslationDto
            {
                LanguageCode = t.Language?.Code,
                Text = t.Text,
                AnswerChoices = t.AnswerChoices?.ToList() ?? new List<string>(),
                MediaText = t.MediaText
            }).ToList() ?? new List<QuestionTranslationDto>(),
            EndingAnswers = question.EndingAnswers?.Select(ea => new EndingAnswerDto
            {
                EndingAnswerId = ea.EndingAnswerId,
                Translations = ea.Translations?.Select(t => new EndingAnswerTranslationDto
                {
                    LanguageCode = t.Language?.Code,
                    Value = t.Value
                }).ToList() ?? new List<EndingAnswerTranslationDto>()
            }).ToList() ?? new List<EndingAnswerDto>()
        };
}
