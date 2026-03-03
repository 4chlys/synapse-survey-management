using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Models;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class CategoryController : Controller
{
    private readonly CategoryService _categoryService;
    
    public CategoryController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public IActionResult Index()
    {
        var categories = _categoryService.GetCategories();
        return View(categories);
    }
}