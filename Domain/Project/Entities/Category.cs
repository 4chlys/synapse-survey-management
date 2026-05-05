using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Category
{
    public int CategoryId { get; set; }

    [Required] public Project Project { get; set; }

    public ICollection<CategoryTranslation> Translations { get; set; } = new List<CategoryTranslation>();

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
}
