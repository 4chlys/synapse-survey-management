using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SingleChoiceResponseValidator : IResponseValidator
{
    public QuestionFormat Format => QuestionFormat.SingleChoice;

    public void Validate(string value, Question question)
    {
        var allChoices = question.Translations?
            .SelectMany(t => t.AnswerChoices ?? Enumerable.Empty<string>())
            ?? Enumerable.Empty<string>();
        if (!allChoices.Contains(value))
            throw new ArgumentException("Invalid answer: choice is not in the list.", nameof(value));
    }
}
