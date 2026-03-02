namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Section(string name, string description)
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}