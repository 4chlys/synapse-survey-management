using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IResponseValidator
{
    QuestionFormat Format { get; }
    void Validate(string value, Question question, string languageCode);
}

public interface IResponseValidatorFactory
{
    IResponseValidator GetValidator(QuestionFormat format);
}
