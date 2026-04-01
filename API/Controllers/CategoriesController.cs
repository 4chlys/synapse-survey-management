using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/categories")]
public class CategoriesController(ICategoryManager categoryManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int projectId)
    {
        var categories = (await categoryManager.GetCategoriesAsync(projectId)).ToList();
        var ideaCounts = await categoryManager.GetIdeaCountsByCategoryAsync(
            categories.Select(c => c.CategoryId));

        var dtos = categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            Name = c.Name,
            Description = c.Description,
            IdeaCount = ideaCounts.GetValueOrDefault(c.CategoryId, 0)
        });

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(int id)
    {
        var category = await categoryManager.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();

        var dto = new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            IdeaCount = await categoryManager.GetIdeaCountAsync(id)
        };
        return Ok(dto);
    }
}