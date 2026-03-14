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

    public async Task<List<CategoryViewModel>> GetCategoriesAsync(int projectId)
    {
        return await _httpClient.GetFromJsonAsync<List<CategoryViewModel>>(
            $"api/projects/{projectId}/categories") ?? new List<CategoryViewModel>();
    }

    public async Task<CategoryViewModel> GetCategoryByIdAsync(int categoryId, int projectId)
    {
        return await _httpClient.GetFromJsonAsync<CategoryViewModel>(
            $"api/projects/{projectId}/categories/{categoryId}");
    }
}