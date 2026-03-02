namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Response(string value, Question question, Participant participant)
{
    public string Value { get; set; } = value;
    
    public Question Question { get; set; } = question;
    public Participant Participant { get; set; } = participant;
}