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

    public async Task<IActionResult> Index(int projectId, int categoryId)
    {
        ViewData["ProjectId"] = projectId;
        ViewData["CategoryId"] = categoryId;
        ViewData["ShowModerationPopup"] = true;
        ViewBag.Emojis = await _emojiService.GetEmojisAsync();

        var category = await _categoryService.GetCategoryByIdAsync(categoryId, projectId);
        if (category == null) return NotFound();

        var ideas = await _ideaService.GetIdeasByCategoryAsync(categoryId);
        ViewBag.Category = category;
        return View(ideas);
    }

    public async Task<IActionResult> Detail(int projectId, int categoryId, int ideaId)
    {
        var idea = await _ideaService.GetIdeaByIdAsync(categoryId, ideaId);
        if (idea == null) return NotFound();

        var comments = await _ideaService.GetCommentsByIdeaAsync(categoryId, ideaId);
        var category = await _categoryService.GetCategoryByIdAsync(categoryId, projectId);

        ViewBag.Category = category;
        ViewBag.ProjectId = projectId;
        ViewBag.CategoryId = categoryId;
        ViewBag.Comments = comments;
        ViewBag.Emojis = await _emojiService.GetEmojisAsync();

        return View(idea);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIdea(int projectId, int categoryId, string description)
    {
        var success = await _ideaService.CreateIdeaAsync(categoryId, description);
        if (!success) return BadRequest("Failed to create idea");
        return RedirectToAction("Index", new { projectId, categoryId });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int projectId, int categoryId, int ideaId, string text)
    {
        var success = await _ideaService.AddCommentAsync(categoryId, ideaId, text);
        if (!success) return BadRequest("Failed to add comment");
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> AddIconComment(int projectId, int categoryId, int ideaId, int emojiId, string returnPage)
    {
        var success = await _emojiService.AddIconCommentAsync(categoryId, ideaId, emojiId);
        if (!success) return BadRequest("Failed to add emoji");

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> AddCommentEmoji(int projectId, int categoryId, int ideaId, int commentId, int emojiId)
    {
        var success = await _emojiService.AddCommentEmojiAsync(categoryId, ideaId, commentId, emojiId);
        if (!success) return BadRequest("Failed to add emoji to comment");
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }
    
}