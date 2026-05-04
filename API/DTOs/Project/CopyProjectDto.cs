using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public class CopyProjectDto
{
    [Required, MaxLength(100)] public string NewName { get; set; }
    [MaxLength(30)]            public string NewShortForm { get; set; }
    [Range(1, int.MaxValue)]   public int PlatformId { get; set; }
}
