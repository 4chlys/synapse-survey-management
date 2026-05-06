namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Section
{
    public int SectionId { get; set; }
    public Questionnaire Questionnaire { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SectionTranslation> Translations { get; set; } = new List<SectionTranslation>();
}
