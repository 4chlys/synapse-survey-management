using System.ComponentModel.DataAnnotations;
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

        var dto = MapToDto(questionnaire);
        return Ok(dto);
    }

    [HttpGet("questions")]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions(int projectId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdReadOnlyAsync(projectId);
        if (questionnaire is null)
            return NotFound();

        var questions = await questionnaireManager.GetQuestionsForProjectAsync(projectId);
        var dtos = questions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpPost("questions")]
    public async Task<ActionResult<QuestionDto>> AddQuestion(int projectId,
        CreateQuestionDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<QuestionFormat>(dto.QuestionFormat, out var format))
            return BadRequest("Invalid QuestionFormat.");

        var (mediaValid, canonicalUrl, mediaError) = MediaUrlValidator.Validate(
            dto.MediaUrl, dto.MediaType, dto.MediaIsEmbed, GcsBucket, env.IsDevelopment());
        if (!mediaValid)
            return BadRequest(mediaError);

        try
        {
            var question = await questionnaireManager.AddQuestionAsync(projectId,
                dto.Text, format, dto.IsMandatory, dto.AnswerChoices?.ToList(), dto.SectionId,
                dto.ParentQuestionId, dto.ParentTriggerValue, dto.EndingAnswers?.ToList(),
                canonicalUrl, dto.MediaType, dto.MediaIsEmbed, dto.MediaText, ct);
            if (question is null)
                return NotFound();

            return Created(string.Empty, MapToDto(question));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
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

        var success = await questionnaireManager.UpdateQuestionAsync(questionId,
            dto.Text, format, dto.IsMandatory, dto.AnswerChoices?.ToList(), dto.SectionId,
            dto.EndingAnswers?.ToList(),
            canonicalUrl, dto.MediaType, dto.MediaIsEmbed, dto.MediaText, ct);
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
    public async Task<ActionResult<QuestionDto>> MoveQuestion(int projectId, int questionId, MoveQuestionDto dto,
        CancellationToken ct)
    {
        if (!await questionnaireManager.QuestionBelongsToProjectAsync(questionId, projectId))
            return NotFound();

        try
        {
            var moved = await questionnaireManager.MoveQuestionAsync(
                questionId, dto.ParentQuestionId, dto.ParentTriggerValue, dto.Position, ct);
            if (moved is null)
                return NotFound();
            return Ok(MapToDto(moved));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
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

    private static QuestionnaireDto MapToDto(Questionnaire questionnaire)
    {
        return new QuestionnaireDto
        {
            QuestionnaireId = questionnaire.QuestionnaireId,
            Questions = questionnaire.Questions
                .OrderBy(q => q.Position)
                .Select(MapToDto)
                .ToList()
        };
    }

    private static QuestionDto MapToDto(Question question)
    {
        return new QuestionDto
        {
            QuestionId = question.QuestionId,
            Text = question.Text,
            QuestionFormat = question.QuestionFormat.ToString(),
            AnswerChoices = question.AnswerChoices.ToList(),
            IsMandatory = question.IsMandatory,
            Position = question.Position,
            ParentQuestionId = question.ParentQuestion?.QuestionId,
            ParentTriggerValue = question.ParentTriggerValue,
            EndingAnswers = question.EndingAnswers.Select(e => e.Value).ToList(),
            SectionName = question.Section?.Name,
            SectionId = question.Section?.SectionId,
            MediaUrl = question.MediaUrl,
            MediaType = question.MediaType,
            MediaIsEmbed = question.MediaIsEmbed,
            MediaText = question.MediaText
        };
    }
}
