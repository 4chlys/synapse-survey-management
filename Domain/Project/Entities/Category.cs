namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public Project Project { get; set; }

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
}