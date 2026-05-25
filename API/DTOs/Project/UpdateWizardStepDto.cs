using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;

public record UpdateWizardStepDto
{
    [Range(1, 3)] public int Step { get; init; }
}
