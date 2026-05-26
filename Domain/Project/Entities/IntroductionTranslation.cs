using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class IntroductionTranslation : IHasLanguage
{
    [Required] public Introduction Introduction { get; set; }
    [Required] public Language Language { get; set; }
    [Required] [MaxLength(5000)] public string Text { get; set; }
}
