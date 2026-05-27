using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class MultipleChoiceResponseValidator : IResponseValidator
{
    public QuestionFormat Format => QuestionFormat.MultipleChoice;

    public void Validate(string value, Question question, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var selected = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var choices = (TranslationPicker.Pick(question.Translations, languageCode)?.AnswerChoices
                       ?? Enumerable.Empty<string>()).ToHashSet();
        foreach (var choice in selected)
        {
            if (!choices.Contains(choice))
                throw new ArgumentException($"Invalid answer: '{choice}' is not in the list.", nameof(value));
        }
    }
}
