using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IManager _manager;

    public CategoriesController(IManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CategoryDto>> GetCategories()
    {
        var categories = _manager.GetCategories();
        var dtos = categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            Name = c.Name,
            Description = c.Description,
            IdeaCount = c.Ideas.Count
        });
        return Ok(dtos);
    }
}
