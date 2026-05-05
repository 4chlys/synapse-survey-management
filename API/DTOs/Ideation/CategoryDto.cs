namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int IdeaCount { get; set; }
    public string LanguageCode { get; set; }
}
