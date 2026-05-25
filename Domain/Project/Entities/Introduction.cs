using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Introduction
{
    public int IntroductionId { get; set; }

    [Required] public Project Project { get; set; }

    public ICollection<IntroductionTranslation> Translations { get; set; } = new List<IntroductionTranslation>();
}
