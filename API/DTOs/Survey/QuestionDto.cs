namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class QuestionDto
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public string QuestionFormat { get; set; }
    public List<string> AnswerChoices { get; set; } = new();
    public bool IsMandatory { get; set; }
    public int Position { get; set; }
    public List<BranchRuleDto> BranchRules { get; set; } = new();
    public string SectionName { get; set; }
}

public class BranchRuleDto
{
    public string TriggerValue { get; set; }
    public int? TargetQuestionId { get; set; }
    public bool SkipToEnd { get; set; }
}
