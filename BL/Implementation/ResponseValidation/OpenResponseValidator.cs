using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class OpenResponseValidator : IResponseValidator
{
    public QuestionFormat Format => QuestionFormat.Open;

    public void Validate(string value, Question question)
    {
        // Open questions accept any text — no additional validation needed.
    }
}
