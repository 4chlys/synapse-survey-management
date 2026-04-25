using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories")]
public class CategoriesController(
    ICategoryManager categoryManager,
    IIdeaManager ideaManager,
    IProjectManager projectManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int projectId)
    {
        var categories = (await categoryManager.GetCategoriesForProjectReadOnlyAsync(projectId)).ToList();
        var counts = await ideaManager.CountByCategoriesAsync(categories.Select(c => c.CategoryId));
        return Ok(categories.Select(c => MapToDto(c, counts.GetValueOrDefault(c.CategoryId, 0))));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(int id)
    {
        var category = await categoryManager.GetCategoryByIdReadOnlyAsync(id);
        if (category is null) return NotFound();
        var count = await ideaManager.CountByCategoryAsync(id);
        return Ok(MapToDto(category, count));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(int projectId, [FromBody] CategoryRequest dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await projectManager.GetProjectByIdForUpdateAsync(projectId);
        if (project is null)
            return NotFound();

        var category = await categoryManager.CreateCategoryAsync(project, dto.Name, dto.Description);
        return Created(string.Empty, MapToDto(category, 0));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequest dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await categoryManager.UpdateCategoryAsync(id, dto.Name, dto.Description);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var success = await categoryManager.DeleteCategoryAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    private static CategoryDto MapToDto(Category category, int ideaCount) => new()
    {
        CategoryId = category.CategoryId,
        Name = category.Name,
        Description = category.Description,
        IdeaCount = ideaCount
    };
}
