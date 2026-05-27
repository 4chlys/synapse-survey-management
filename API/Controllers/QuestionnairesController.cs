using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.API.Helpers;
using SynapseEnqueteManagement.API.Options;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/questionnaire")]
public class QuestionnairesController(
    IQuestionnaireManager questionnaireManager,
    ISurveyFlowManager surveyFlowManager,
    IParticipantManager participantManager,
    IProjectManager projectManager,
    IQuestionMediaCleanupService mediaCleanup,
    IOptions<StorageOptions> storageOptions,
    IWebHostEnvironment env)
    : ParticipantControllerBase(participantManager, projectManager)
{
    private readonly string _gcsBucket = storageOptions.Value.BucketName;

    [HttpGet]
    public async Task<ActionResult<QuestionnaireDto>> GetQuestionnaire(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        var dto = QuestionDtoMapper.ToQuestionnaireDto(questionnaire);
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
        var dtos = questions.Select(q => QuestionDtoMapper.ToDto(q, languageCode)).ToList();
        return Ok(dtos);
    }

    [HttpPost("questions")]
    public async Task<ActionResult<AdminQuestionDto>> AddQuestion(int projectId,
        CreateQuestionDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<QuestionFormat>(dto.QuestionFormat, out var format))
            return BadRequest("Invalid QuestionFormat.");

        var (mediaValid, canonicalUrl, mediaError) = MediaUrlValidator.Validate(
            dto.MediaUrl, dto.MediaType, dto.MediaIsEmbed, _gcsBucket, env.IsDevelopment());
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

        return Created(string.Empty, QuestionDtoMapper.ToAdminDto(question));
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
            dto.MediaUrl, dto.MediaType, dto.MediaIsEmbed, _gcsBucket, env.IsDevelopment());
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
        await mediaCleanup.DeleteIfOrphanedAsync(oldMediaUrl, canonicalUrl, existing?.MediaIsEmbed ?? false);

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
        await mediaCleanup.DeleteOnRemovalAsync(mediaUrl, isEmbed);

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

        return Ok(QuestionDtoMapper.ToAdminDto(moved));
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
}
