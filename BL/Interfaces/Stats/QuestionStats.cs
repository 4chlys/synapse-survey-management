using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.BL.Interfaces;

public sealed record QuestionLookup(int QuestionId, string Text);

public sealed record SingleChoiceStats(int QuestionId, int TotalResponses, ICollection<OptionCount> Counts);
public sealed record MultipleChoiceStats(int QuestionId, int TotalResponses, ICollection<OptionCount> Counts);
public sealed record LikertStats(int QuestionId, int TotalResponses, ICollection<LikertPointCount> Distribution);
public sealed record OpenAnswerPage(int QuestionId, int TotalAnswers, ICollection<OpenAnswer> Answers);
