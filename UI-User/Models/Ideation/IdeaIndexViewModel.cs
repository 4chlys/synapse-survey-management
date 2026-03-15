namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public class IdeaIndexViewModel
{
    public int ProjectId { get; set; }
    public int CategoryId { get; set; }
    public CategoryViewModel Category { get; set; } = null!;
    public List<IdeaViewModel> Ideas { get; set; } = new();
    public List<EmojiViewModel> Emojis { get; set; } = new();
    public bool ShowContactInfoPopup { get; set; }
    public bool RequestContactInfo { get; set; }
}
