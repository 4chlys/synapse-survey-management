using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class CategoryService
{
    public List<Category> GetCategories()
    {
        return new List<Category>
            {
                new Category { Id = 1, Name = "Campus welzijn", Description = "Welzijn op school", IdeaCount = 55 },
                new Category { Id = 2, Name = "Thuis welzijn", Description = "Welzijn thuis en in je vrije tijd",IdeaCount = 323 },
                new Category { Id = 3, Name = "Online welzijn", Description = "Gezond omgaan met social media en online", IdeaCount = 19 }
            };
        
    }
}