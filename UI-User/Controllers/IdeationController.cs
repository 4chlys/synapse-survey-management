using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class IdeationController : Controller
{
    private readonly CategoryService _categoryService;
    private readonly IdeaService _ideaService;
    private readonly EmojiService _emojiService;

    public IdeationController(CategoryService categoryService, IdeaService ideaService, EmojiService emojiService)
    {
        _categoryService = categoryService;
        _ideaService = ideaService;
        _emojiService = emojiService;
    }

    public IActionResult Index(int projectId, int categoryId)
    {
        ViewData["ProjectId"] = projectId;
        ViewData["CategoryId"] = categoryId;
        ViewData["ShowModerationPopup"] = true;
        ViewBag.Emojis = _emojiService.GetEmojis();

        var category = _categoryService.GetCategoryById(categoryId);
        if (category == null) return NotFound();

        var ideas = _ideaService.GetIdeasByCategory(categoryId);
        ViewBag.Category = category;
        return View(ideas);
    }

    public IActionResult Detail(int projectId, int categoryId, int ideaId)
    {
        var idea = _ideaService.GetIdeaById(categoryId, ideaId);
        if (idea == null) return NotFound();

        var comments = _ideaService.GetCommentsByIdea(categoryId, ideaId);
        var category = _categoryService.GetCategoryById(categoryId);

        ViewBag.Category = category;
        ViewBag.ProjectId = projectId;
        ViewBag.CategoryId = categoryId;
        ViewBag.Comments = comments;
        ViewBag.Emojis = _emojiService.GetEmojis();

        return View(idea);
    }

    [HttpPost]
    public IActionResult AddComment(int projectId, int categoryId, int ideaId, string text)
    {
        _ideaService.AddComment(ideaId, text);
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public IActionResult AddIconComment(int projectId, int categoryId, int ideaId, int emojiId, string returnPage)
    {
        _emojiService.AddIconComment(ideaId, emojiId);

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public IActionResult AddCommentEmoji(int projectId, int categoryId, int ideaId, int commentId, int emojiId)
    {
        _emojiService.AddCommentEmoji(ideaId, commentId, emojiId);
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public IActionResult CreateIdea(int projectId, int categoryId, string description)
    {
        _ideaService.CreateIdea(categoryId, description);
        return RedirectToAction("Index", new { projectId, categoryId });
    }
}