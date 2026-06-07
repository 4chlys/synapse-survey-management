using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

// Multi-language create payload. Structural fields (format, mandatory, section,
// parent linkage, media settings) are language-agnostic; per-language content
// lives in Translations and, when authored, EndingAnswerSlots. AnswerChoices
// per language are submitted as newline-separated strings (one slot per line) —
// the slot-row UI in project-questions.ts joins each row's per-language input
// values into the matching language's AnswerChoicesRaw before submit.
public record CreateQuestionViewModel
{
    public QuestionFormat QuestionFormat { get; init; }

    public bool IsMandatory { get; init; }

    public int? SectionId { get; init; }

    public int? ParentQuestionId { get; init; }
    public int? ChoiceIndex { get; init; }

    public string MediaUrl { get; init; }
    public QuestionMediaType? MediaType { get; init; }
    public bool MediaIsEmbed { get; init; }

    public List<QuestionTranslationFormViewModel> Translations { get; init; } = new();

    // Modal does not author ending answers (those use the tree's "Ends survey"
    // toggle), so this stays null when not posted — which the service maps to
    // "preserve existing endings". Posting an empty list explicitly clears them
    // (used by the tree-toggle controller when all toggles are unchecked).
    public List<EndingAnswerSlotFormViewModel> EndingAnswerSlots { get; init; }
}

public record QuestionTranslationFormViewModel
{
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; init; }

    [Required]
    [MaxLength(500)]
    public string Text { get; init; }

    [MaxLength(2000)]
    public string MediaText { get; init; }

    // Newline-separated answer-choice values for this language, one per slot.
    // Slot count must match across all supported languages (enforced client-side).
    public string AnswerChoicesRaw { get; init; }

    public List<string> GetAnswerChoices() =>
        (AnswerChoicesRaw ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}

public record EndingAnswerSlotFormViewModel
{
    public List<EndingAnswerTranslationFormViewModel> Translations { get; init; } = new();
}

public record EndingAnswerTranslationFormViewModel
{
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; init; }

    [MaxLength(500)]
    public string Value { get; init; }
}
