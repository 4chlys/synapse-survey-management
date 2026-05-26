using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class IdeaSummaryJob : IHasCreatedAt
{
    public Guid IdeaSummaryJobId { get; set; }
    public int ProjectPlatformId { get; set; }
    public int ProjectId { get; set; }
    public IdeaSummaryJobStatus Status { get; set; } = IdeaSummaryJobStatus.Pending;
    [Required] public string FilterJson { get; set; }
    public IdeaSummaryExportFormat ExportFormat { get; set; }
    public string OutputPath { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string RequestedBy { get; set; }
    public byte[] RowVersion { get; set; }
}