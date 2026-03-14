using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Section
{
    public int SectionId { get; set; }

    [Required] [MaxLength(20)] public string Name { get; set; }

    [MaxLength(100)] public string Description { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}