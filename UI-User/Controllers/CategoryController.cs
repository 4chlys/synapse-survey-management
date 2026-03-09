using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class CategoryController : Controller
{
    private readonly CategoryService _categoryService;

    public CategoryController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public IActionResult Index(int projectId)
    {
        ViewData["ProjectId"] = projectId;
        var categories = _categoryService.GetCategories();
        return View(categories);
    }
}