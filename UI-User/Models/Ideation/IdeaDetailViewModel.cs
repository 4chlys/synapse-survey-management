namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record IdeaDetailViewModel
{
    public int ProjectId { get; init; }
    public int CategoryId { get; init; }
    public CategoryViewModel Category { get; init; } = null!;
    public IdeaViewModel Idea { get; init; } = null!;
    public ICollection<CommentViewModel> Comments { get; init; } = new List<CommentViewModel>();
    public ICollection<EmojiViewModel> Emojis { get; init; } = new List<EmojiViewModel>();
    public bool AllowCrossLanguageIdeas { get; init; }
    public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();
    public string ParticipantLanguageCode { get; init; }
}
