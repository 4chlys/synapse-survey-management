using SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public record WidgetSlot(string WidgetType, int? QuestionId);

public static class WidgetSlotValidator
{
    public static readonly string[] DefaultLayout =
    [
        "ViewCount", "CompletionCount", "CompletionRate",
        "ParticipationTimeline", "TopIdeas", "TopCategories",
        "SurveyDistribution", "IdeaFeed", "ModerationQueue"
    ];

    public static readonly string[] AllWidgetTypes =
    [
        "ViewCount", "CompletionCount", "CompletionRate",
        "ParticipationTimeline", "TopCategories", "TopIdeas",
        "SurveyDistribution", "IdeaFeed", "ModerationQueue",
        "QuestionSinglePie", "QuestionMultiBar", "QuestionLikertBar",
        "SocialMediaPost"
    ];

    public static WidgetSlot[] SanitizeSlots(WidgetSlotInput[] source)
    {
        source ??= [];
        return Enumerable.Range(0, 9)
            .Select(i =>
            {
                var input = i < source.Length ? source[i] : null;
                var type = input is not null && AllWidgetTypes.Contains(input.WidgetType)
                    ? input.WidgetType
                    : DefaultLayout[i];
                var isQuestionBound = type is "QuestionSinglePie" or "QuestionMultiBar" or "QuestionLikertBar";
                return new WidgetSlot(type, isQuestionBound ? input?.QuestionId : null);
            })
            .ToArray();
    }
}
