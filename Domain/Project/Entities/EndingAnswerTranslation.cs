using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class EndingAnswerTranslation
{
    [Required] public EndingAnswer EndingAnswer { get; set; }
    [Required] public Language Language { get; set; }
    [Required] [MaxLength(500)] public string Value { get; set; }
}
