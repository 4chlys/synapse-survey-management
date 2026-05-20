using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Models;

public sealed class SummaryFilterParams
{
    public int? CategoryId { get; init; }
    public string Status { get; init; }
    public string LanguageCode { get; init; }
    public IdeaSummarySamplingStrategy SamplingStrategy { get; init; }
    public bool SelectAll { get; init; }
}