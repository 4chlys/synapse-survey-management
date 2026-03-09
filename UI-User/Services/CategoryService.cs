using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class CategoryService
{
    private readonly HttpClient _httpClient;

    public CategoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<Category> GetCategories()
    {
        return GetCategoriesAsync().GetAwaiter().GetResult();
    }

    public Category GetCategoryById(int id)
    {
        return GetCategoryByIdAsync(id).GetAwaiter().GetResult();
    }

    private async Task<List<Category>> GetCategoriesAsync()
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<ApiCategoryDto>>("api/categories");
        if (dtos is null) return new List<Category>();

        return dtos.Select(d => new Category
        {
            Id = d.CategoryId,
            Name = d.Name,
            Description = d.Description,
            IdeaCount = d.IdeaCount
        }).ToList();
    }

    private async Task<Category> GetCategoryByIdAsync(int id)
    {
        var dto = await _httpClient.GetFromJsonAsync<ApiCategoryDto>($"api/categories/{id}");
        if (dto is null) return null;

        return new Category
        {
            Id = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            IdeaCount = dto.IdeaCount
        };
    }

    private class ApiCategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int IdeaCount { get; set; }
    }
}