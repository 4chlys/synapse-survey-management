namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Section
{
    public int SectionId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    
}