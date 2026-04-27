using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Domain.Platform.Entities;

public class ProjectPlatform : IHasCreatedAt
{
    public int ProjectPlatformId { get; set; }
    public DateTime CreatedAt { get; set; }

    [Required] [MaxLength(100)] public string Name { get; set; }

    [MaxLength(500)]
    public string LogoUrl { get; set; }

    [MaxLength(500)]
    public string WebsiteUrl { get; set; }
}
