using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Category
{
    public int CategoryId { get; set; }

    [Required] [MaxLength(100)] public string Name { get; set; }

    [MaxLength(1000)] public string Description { get; set; }

    [Required] public Project Project { get; set; }

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
}