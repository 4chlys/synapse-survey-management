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

    public async Task<List<Category>> GetCategoriesAsync(int projectId)
    {
        return await _httpClient.GetFromJsonAsync<List<Category>>(
            $"api/projects/{projectId}/categories") ?? new List<Category>();
    }

    public async Task<Category> GetCategoryByIdAsync(int id, int projectId)
    {
        return await _httpClient.GetFromJsonAsync<Category>($"api/projects/{projectId}/categories/{id}");
    }
}