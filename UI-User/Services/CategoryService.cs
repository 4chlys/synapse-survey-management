using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class CategoryService
{
    public List<Category> GetCategories()
    {
        return new List<Category>
            {
                new Category { Id = 1, Name = "Campus welzijn", IdeaCount = 55 },
                new Category { Id = 2, Name = "Thuis welzijn", IdeaCount = 323 },
                new Category { Id = 3, Name = "Online welzijn", IdeaCount = 19 }
            };
        
    }
}