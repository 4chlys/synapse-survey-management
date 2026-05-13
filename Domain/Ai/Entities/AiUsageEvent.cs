using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.BL.Domain.Ai.Entities;

public class AiUsageEvent : IHasCreatedAt
{
    public int AiUsageEventId { get; set; }
    public DateTime CreatedAt { get; set; }

    [Required] public Project.Entities.Project Project { get; set; }

    [Required] public AiServiceKind Service { get; set; }

    [Required] [MaxLength(100)] public string Model { get; set; }

    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int RequestCount { get; set; }
    public decimal CostEur { get; set; }
}
