using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SingleChoiceResponseValidator : IResponseValidator
{
    public QuestionFormat Format => QuestionFormat.SingleChoice;

    public void Validate(string value, Question question)
    {
        if (!question.AnswerChoices.Contains(value))
            throw new ArgumentException("Invalid answer: choice is not in the list.", nameof(value));
    }
}
