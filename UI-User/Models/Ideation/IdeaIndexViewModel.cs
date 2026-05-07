namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public class IdeaIndexViewModel
{
    public int ProjectId { get; set; }
    public int CategoryId { get; set; }
    public CategoryViewModel Category { get; set; } = null!;
    public List<IdeaViewModel> Ideas { get; set; } = new();
    public List<EmojiViewModel> Emojis { get; set; } = new();
    public bool ShowContactInfoPopup { get; set; }
    public string CurrentSort { get; set; } = "diverse";
    public bool HasOwnIdeas { get; set; }
    public bool ExtraIdeasReplaceExisting { get; set; }
    public int MaxExtraIdeaRequests { get; set; }
    public int RemainingExtraRequests { get; set; }
    public bool AllowCrossLanguageIdeas { get; set; }
    public ICollection<string> SupportedLanguageCodes { get; set; } = new List<string>();
    public string ParticipantLanguageCode { get; set; }
}
