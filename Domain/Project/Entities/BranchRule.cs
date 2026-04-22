namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class BranchRule
{
    public int BranchRuleId { get; set; }

    // The question whose answer triggers this branch
    public Question SourceQuestion { get; set; }

    // The answer value that triggers the branch
    // For MultipleChoice: triggers if this value is among selected choices
    public string TriggerValue { get; set; }

    // Where to go: either a specific question or end of survey
    public Question TargetQuestion { get; set; }

    // If true, skip to end of survey instead of targeting a question
    public bool SkipToEnd { get; set; }
}