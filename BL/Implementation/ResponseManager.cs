using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ResponseManager(IRepository repository, IResponseValidatorFactory validatorFactory) : IResponseManager
{
    public async Task<Response> AddResponseAsync(string value, Question question, Participant participant)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Response must not exceed 500 characters.", nameof(value));

        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question);

        var response = new Response { Value = value, Question = question, Participant = participant, CreatedAt = DateTime.UtcNow };
        return await repository.CreateAsync(response);
    }
}