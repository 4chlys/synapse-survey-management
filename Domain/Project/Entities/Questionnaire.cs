using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Questionnaire
{
    public int QuestionnaireId { get; set; }
    [Required] public Project Project { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}