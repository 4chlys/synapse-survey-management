using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CategoryManager(IRepository repository) : ICategoryManager
{
    public async Task<IEnumerable<Category>> GetCategoriesAsync(int projectId)
    {
        return await repository.ReadAsync<Category>(
            filters: [c => c.Project.ProjectId == projectId]
        );
    }
    public async Task<Category> CreateCategoryAsync(int projectId, string name,
        string description)
    {
        var project = await repository.ReadByIdAsync<Project>(projectId);
        if (project is null) return null;

        var category = new Category
        {
            Name = name,
            Description = description,
            Project = project
        };
        return await repository.CreateAsync(category);
    }

    public async Task<bool> UpdateCategoryAsync(int categoryId, string name, string
        description)
    {
        var category = await repository.ReadByIdAsync<Category>(categoryId);
        if (category is null) return false;

        category.Name = name;
        category.Description = description;
        return await repository.UpdateAsync(category);
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        return await repository.DeleteByIdAsync<Category>(categoryId);
    }

    public async Task<Category> GetCategoryByIdAsync(int id)
    {
        return await repository.ReadByIdAsync<Category>(id);
    }

    public async Task<int> GetIdeaCountAsync(int categoryId)
    {
        return await repository.CountAsync<Idea>(
            filters: [i => i.Category.CategoryId == categoryId]
        );
    }
}