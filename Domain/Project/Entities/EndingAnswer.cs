using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class EndingAnswer
{
    public int EndingAnswerId { get; set; }

    public Question Question { get; set; }

    [Required] [MaxLength(500)] public string Value { get; set; }
}
