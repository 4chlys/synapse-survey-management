namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class MoveQuestionDto
{
    public int? ParentQuestionId { get; set; }
    public string ParentTriggerValue { get; set; }
    public int Position { get; set; }
}
