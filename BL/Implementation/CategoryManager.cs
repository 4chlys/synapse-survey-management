using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CategoryManager(
    ICategoryRepository categoryRepository,
    IProjectRepository projectRepository,
    IIdeaRepository ideaRepository,
    IUnitOfWork uow) : ICategoryManager
{
    public async Task<IEnumerable<(Category Category, int IdeaCount)>> GetCategoriesAndTheirIdeaCountsAsync(int projectId)
    {
        var categories = (await categoryRepository.ReadByProjectIdReadOnlyAsync(projectId)).ToList();
        var counts = await ideaRepository.CountByCategoriesAsync(categories.Select(c => c.CategoryId));
        return categories.Select(c => (c, counts.GetValueOrDefault(c.CategoryId, 0)));
    }

    public async Task<Category> CreateCategoryAsync(int projectId, string name, string description)
    {
        var project = await projectRepository.ReadByIdForUpdateAsync(projectId);
        if (project is null) return null;

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

    public async Task<(Category Category, int IdeaCount)> GetCategoryAndItsIdeaCountByIdAsync(int id)
    {
        var category = await categoryRepository.ReadByIdReadOnlyAsync(id)
                       ?? throw new KeyNotFoundException($"Category {id} not found.");
        var count = await ideaRepository.CountByCategoryAsync(id);
        return (category, count);
    }
}
