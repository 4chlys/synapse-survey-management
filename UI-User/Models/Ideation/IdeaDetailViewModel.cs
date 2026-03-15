namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public class IdeaDetailViewModel
{
    public int ProjectId { get; set; }
    public int CategoryId { get; set; }
    public CategoryViewModel Category { get; set; } = null!;
    public IdeaViewModel Idea { get; set; } = null!;
    public List<CommentViewModel> Comments { get; set; } = new();
    public List<EmojiViewModel> Emojis { get; set; } = new();
}
