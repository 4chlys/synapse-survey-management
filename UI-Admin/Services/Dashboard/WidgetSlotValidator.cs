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

    // Validates form input and produces one WidgetSlot per posted entry. Unknown widget types
    // fall back to the per-index default (or the last default for positions beyond the default
    // layout). QuestionId is dropped unless the slot's type is a question-bound widget.
    public static WidgetSlot[] SanitizeSlots(WidgetSlotInput[] source)
    {
        source ??= [];
        return source
            .Select((input, i) =>
            {
                var type = input is not null && AllWidgetTypes.Contains(input.WidgetType)
                    ? input.WidgetType
                    : DefaultLayout[Math.Min(i, DefaultLayout.Length - 1)];
                var isQuestionBound = type is "QuestionSinglePie" or "QuestionMultiBar" or "QuestionLikertBar";
                return new WidgetSlot(type, isQuestionBound ? input?.QuestionId : null);
            })
            .ToArray();
    }
}
