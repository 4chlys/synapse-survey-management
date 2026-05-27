using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Services;

public static class ResponseDtoMapper
{
    public static ResponseDto ToDto(Response response) => new()
    {
        ResponseId = response.ResponseId,
        Value = response.Value,
        QuestionId = response.Question?.QuestionId ?? 0,
        ParticipantId = response.Participant?.ParticipantId ?? 0,
        LanguageCode = response.Language?.Code
    };
}
