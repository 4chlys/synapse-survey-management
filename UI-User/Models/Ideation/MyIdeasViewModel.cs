namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public class MyIdeasViewModel
{
    public int ProjectId { get; set; }
    public int CategoryId { get; set; }
    public CategoryViewModel Category { get; set; } = null!;
    public List<IdeaViewModel> Ideas { get; set; } = new();
}
