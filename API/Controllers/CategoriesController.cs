using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories")]
public class CategoriesController(ICategoryManager categoryManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int projectId)
    {
        var results = await categoryManager.GetCategoriesAndTheirIdeaCountsAsync(projectId);
        return Ok(results.Select(r => MapToDto(r.Category, r.IdeaCount)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(int id)
    {
        var (category, ideaCount) = await categoryManager.GetCategoryAndItsIdeaCountByIdAsync(id);
        return Ok(MapToDto(category, ideaCount));
    }

    private static CategoryDto MapToDto(Category category, int ideaCount) => new()
    {
        CategoryId = category.CategoryId,
        Name = category.Name,
        Description = category.Description,
        IdeaCount = ideaCount
    };
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(int projectId,
        [FromBody] CategoryRequest dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var category = await categoryManager.CreateCategoryAsync(projectId, dto.Name,
            dto.Description);
        if (category is null)
            return NotFound();

        return Created(string.Empty, MapToDto(category, 0));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody]
        CategoryRequest dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await categoryManager.UpdateCategoryAsync(id, dto.Name,
            dto.Description);
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
}