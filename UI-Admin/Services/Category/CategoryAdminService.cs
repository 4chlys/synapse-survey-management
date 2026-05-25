using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Category;

namespace SynapseEnqueteManagement.UI.Admin.Services.Category;

public class CategoryAdminService(HttpClient httpClient)
{
    public async Task<List<CategoryViewModel>> GetCategoriesAsync(int projectId)
    {
        var response = await
            httpClient.GetAsync($"api/projects/{projectId}/categories");
        if (!response.IsSuccessStatusCode)
            return new List<CategoryViewModel>();

        return await
                   response.Content.ReadFromJsonAsync<List<CategoryViewModel>>()
               ?? new List<CategoryViewModel>();
    }

    public async Task CreateCategoryAsync(int projectId, string name, string description)
    {
        var payload = new
        {
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = name, Description = description }
            }
        };
        var response = await
            httpClient.PostAsJsonAsync($"api/projects/{projectId}/categories", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateCategoryAsync(int projectId, int categoryId, string name, string description)
    {
        var payload = new
        {
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = name, Description = description }
            }
        };
        var response = await httpClient.PutAsJsonAsync(
            $"api/projects/{projectId}/categories/{categoryId}", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCategoryAsync(int projectId, int categoryId)
    {
        var response = await httpClient.DeleteAsync(
            $"api/projects/{projectId}/categories/{categoryId}");
        response.EnsureSuccessStatusCode();
    }
}
