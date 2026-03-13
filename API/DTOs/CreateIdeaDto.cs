using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.DTOs;

public class CreateIdeaDto
{
    public string Description { get; set; }
    
    public string ModerationStatus { get; set; } = "Unflagged";

    public ModerationStatus ParsedModerationStatus =>
        Enum.TryParse<ModerationStatus>(ModerationStatus, ignoreCase: true, out var status)
            ? status
            : BL.Domain.Project.Enums.ModerationStatus.Unflagged;
}