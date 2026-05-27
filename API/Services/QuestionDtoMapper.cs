using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Services;

// Maps Question(naire) entities to the survey/admin DTOs. Lives next to ProjectDtoMapper so
// DTO shaping stays out of the controller (its job is HTTP, not translation projection).
public static class QuestionDtoMapper
{
    public static QuestionnaireDto ToQuestionnaireDto(Questionnaire questionnaire)
        => new()
        {
            QuestionnaireId = questionnaire.QuestionnaireId,
            Questions = questionnaire.Questions
                .OrderBy(q => q.Position)
                .Select(q => ToDto(q, null))
                .ToList()
        };

    public static QuestionDto ToDto(Question question, string languageCode)
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

    public static AdminQuestionDto ToAdminDto(Question question)
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
