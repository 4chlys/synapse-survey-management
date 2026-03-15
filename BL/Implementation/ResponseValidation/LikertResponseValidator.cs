using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class LikertResponseValidator : IResponseValidator
{
    public QuestionFormat Format => QuestionFormat.Likert;

    private const int DefaultMax = 10;

    public void Validate(string value, Question question)
    {
        if (!int.TryParse(value, out var numericValue))
            throw new ArgumentException("Likert response must be a number.", nameof(value));

        var max = question.AnswerChoices.Count > 0 ? question.AnswerChoices.Count : DefaultMax;
        if (numericValue < 1 || numericValue > max)
            throw new ArgumentException(
                $"Likert response must be between 1 and {max}.", nameof(value));
    }
}
