using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Response : IHasCreatedAt
{
    public int ResponseId { get; set; }
    public DateTime CreatedAt { get; set; }

    [MaxLength(500)] public string Value { get; set; }

    public Question Question { get; set; }

    public Participant Participant { get; set; }
}