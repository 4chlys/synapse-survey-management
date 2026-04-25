using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CategoryManager(
    ICategoryRepository categoryRepository,
    IUnitOfWork uow) : ICategoryManager
{
    public Task<IEnumerable<Category>> GetCategoriesForProjectReadOnlyAsync(int projectId)
        => categoryRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public async Task<Category> GetCategoryByIdReadOnlyAsync(int id)
        => await categoryRepository.ReadByIdReadOnlyAsync(id)
           ?? throw new KeyNotFoundException($"Category {id} not found.");

    public async Task<Category> CreateCategoryAsync(Project project, string name, string description)
    {
        var category = new Category
        {
            Name = name,
            Description = description,
            Project = project
        };
        var created = await categoryRepository.CreateAsync(category);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<bool> UpdateCategoryAsync(int categoryId, string name, string description)
    {
        var category = await categoryRepository.ReadByIdForUpdateAsync(categoryId);
        if (category is null) return false;

        category.Name = name;
        category.Description = description;
        categoryRepository.Update(category);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var result = await categoryRepository.DeleteByIdAsync(categoryId);
        if (result) await uow.SaveChangesAsync();
        return result;
    }
}
