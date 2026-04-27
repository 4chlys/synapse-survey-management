using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Response : IHasCreatedAt
{
    public int ResponseId { get; set; }
    public DateTime CreatedAt { get; set; }

    [MaxLength(500)] public string Value { get; set; }
    public bool IsCommitted { get; set; }

    [Required] public Question Question { get; set; }

    [Required] public Participant Participant { get; set; }
}