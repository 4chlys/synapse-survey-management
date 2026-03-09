using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class IdeationController : Controller
{
    private readonly CategoryService _categoryService;
    private readonly IdeaService _ideaService;
    
    public IdeationController(CategoryService categoryService, IdeaService ideaService)
    {
        _categoryService = categoryService;
        _ideaService = ideaService;
    }

    public IActionResult Index(int categoryId)
    {
        var category = _categoryService.GetCategoryById(categoryId);
        if (category == null) return NotFound();

        var ideas = _ideaService.GetIdeasByCategory(categoryId);
        
        ViewBag.Category = category;
        return View(ideas);
    }

    [HttpPost]
    public IActionResult CreateIdea(int categoryId, string description)
    {
        _ideaService.CreateIdea(categoryId, description);
        return RedirectToAction("Index", new { categoryId });
    }
}