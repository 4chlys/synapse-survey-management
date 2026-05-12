namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class EndingAnswer
{
    public int EndingAnswerId { get; set; }
    public Question Question { get; set; }
    public ICollection<EndingAnswerTranslation> Translations { get; set; } = new List<EndingAnswerTranslation>();
}
