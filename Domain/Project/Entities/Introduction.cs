using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Introduction
{
    public int IntroductionId { get; set; }

    [Required]
    [MaxLength(5000)]
    public string Text { get; set; }
}