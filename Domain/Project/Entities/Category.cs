namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Category(string name, string description)
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
}