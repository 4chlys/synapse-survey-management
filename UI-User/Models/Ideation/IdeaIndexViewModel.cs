namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record IdeaIndexViewModel
{
    public int ProjectId { get; init; }
    public int CategoryId { get; init; }
    public CategoryViewModel Category { get; init; } = null!;
    public ICollection<IdeaViewModel> Ideas { get; init; } = new List<IdeaViewModel>();
    public ICollection<EmojiViewModel> Emojis { get; init; } = new List<EmojiViewModel>();
    public bool ShowContactInfoPopup { get; init; }
    public string CurrentSort { get; init; } = "diverse";
    public bool HasOwnIdeas { get; init; }
    public bool ExtraIdeasReplaceExisting { get; init; }
    public int MaxExtraIdeaRequests { get; init; }
    public int RemainingExtraRequests { get; init; }
    public bool AllowCrossLanguageIdeas { get; init; }
    public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();
    public string ParticipantLanguageCode { get; init; }
}
