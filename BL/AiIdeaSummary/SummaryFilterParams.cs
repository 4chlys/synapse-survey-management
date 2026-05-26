using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.AiIdeaSummary;

public sealed record SummaryFilterParams
{
    public int? CategoryId { get; init; }
    public string Status { get; init; }
    public string LanguageCode { get; init; }
    public string OutputLanguage { get; init; }
    public IdeaSummarySamplingStrategy SamplingStrategy { get; init; }
    public bool SelectAll { get; init; }
}