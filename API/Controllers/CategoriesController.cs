using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories")]
public class CategoriesController(
    ICategoryManager categoryManager,
    IIdeaManager ideaManager,
    IProjectManager projectManager) : ControllerBase
{
    private static string ActiveCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int projectId)
    {
        var categories = (await categoryManager.GetCategoriesForProjectReadOnlyAsync(projectId)).ToList();
        var lang = ActiveCulture();
        var counts = await ideaManager.CountVisibleByCategoriesAsync(categories.Select(c => c.CategoryId), lang);
        return Ok(categories.Select(c => CategoryDtoMapper.ToDto(c, counts.GetValueOrDefault(c.CategoryId, 0), lang)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(int projectId, int id)
    {
        var category = await categoryManager.GetCategoryByIdWithProjectReadOnlyAsync(id);
        if (category is null || category.Project?.ProjectId != projectId) return NotFound();
        var lang = ActiveCulture();
        var count = await ideaManager.CountVisibleByCategoryAsync(id, lang);
        return Ok(CategoryDtoMapper.ToDto(category, count, lang));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCategoryDto>> CreateCategory(int projectId, CreateCategoryDto dto,
        CancellationToken ct)
    {
        var project = await projectManager.GetProjectByIdForUpdateAsync(projectId);
        if (project is null)
            return NotFound();

        var translations = dto.Translations.Select(t => new CategoryTranslationInput(t.LanguageCode, t.Name, t.Description));
        var category = await categoryManager.CreateCategoryAsync(project, translations, ct);
        return Created(string.Empty, CategoryDtoMapper.ToAdminDto(category, 0));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto dto, CancellationToken ct)
    {
        var translations = dto.Translations.Select(t => new CategoryTranslationInput(t.LanguageCode, t.Name, t.Description));
        var success = await categoryManager.UpdateCategoryAsync(id, translations, ct);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var success = await categoryManager.DeleteCategoryAsync(id, ct);
        if (!success)
            return NotFound();

        return NoContent();
    }

}
