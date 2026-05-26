using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

// Multilingual question shape returned by GET /api/admin/projects/{id}/questions.
// Mirrors API AdminQuestionDto: structural fields are language-agnostic, and
// per-language content (text, answer-choice slot values, media caption, and
// ending-answer slot values) lives inside Translations / EndingAnswers.
// AnswerChoiceCount is the authoritative slot count — every language's
// AnswerChoices list has exactly that many entries (slot-row invariant).
public record AdminQuestionViewModel
{
    public int QuestionId { get; init; }
    public string QuestionFormat { get; init; }
    public bool IsMandatory { get; init; }
    public int Position { get; init; }
    public int AnswerChoiceCount { get; init; }
    public int? ChoiceIndex { get; init; }
    public int? ParentQuestionId { get; init; }
    public int? SectionId { get; init; }
    public string SectionName { get; init; }
    public string MediaUrl { get; init; }
    public QuestionMediaType? MediaType { get; init; }
    public bool MediaIsEmbed { get; init; }
    public ICollection<QuestionTranslationViewModel> Translations { get; init; }
        = new List<QuestionTranslationViewModel>();
    public ICollection<EndingAnswerViewModel> EndingAnswers { get; init; }
        = new List<EndingAnswerViewModel>();
}

public record QuestionTranslationViewModel
{
    public string LanguageCode { get; init; }
    public string Text { get; init; }
    // Slot count must equal the question's AnswerChoiceCount.
    public ICollection<string> AnswerChoices { get; init; } = new List<string>();
    public string MediaText { get; init; }
}

public record EndingAnswerViewModel
{
    public int EndingAnswerId { get; init; }
    public ICollection<EndingAnswerTranslationViewModel> Translations { get; init; }
        = new List<EndingAnswerTranslationViewModel>();
}

public record EndingAnswerTranslationViewModel
{
    public string LanguageCode { get; init; }
    public string Value { get; init; }
}
