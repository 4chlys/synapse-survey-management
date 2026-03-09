using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryManager _categoryManager;

    public CategoriesController(ICategoryManager categoryManager)
    {
        _categoryManager = categoryManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _categoryManager.GetCategoriesAsync();
        var dtos = categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            Name = c.Name,
            Description = c.Description,
            IdeaCount = c.Ideas.Count
        });
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public ActionResult<CategoryDto> GetCategoryById(int id)
    {
        var category = _manager.GetCategoryById(id);
        if (category == null) return NotFound();

        var dto = new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            IdeaCount = category.Ideas.Count
        };
        return Ok(dto);
    }
}