using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record ProjectTranslationViewModel
{
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; init; }

    [MaxLength(100)]
    public string Name { get; init; }

    [MaxLength(100)]
    public string Topic { get; init; }

    [MaxLength(5000, ErrorMessage = "Introduction_TooLong")]
    public string Introduction { get; init; }
}
