namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Response
{
    public int ResponseId { get; set; }
    public string Value { get; set; }

    public Question Question { get; set; }

    public Participant Participant { get; set; }
}