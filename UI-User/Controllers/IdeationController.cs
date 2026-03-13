using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Services;

namespace SynapseEnqueteManagement.UI.User.Controllers;

public class IdeationController(CategoryService categoryService, IdeaService ideaService, CommentService commentService, EmojiService emojiService)
    : Controller
{
    public async Task<IActionResult> Index(int projectId, int categoryId)
    {
        ViewData["ProjectId"] = projectId;
        ViewData["CategoryId"] = categoryId;
        ViewData["ShowModerationPopup"] = true;
        ViewBag.Emojis = await emojiService.GetEmojisAsync();

        var category = await categoryService.GetCategoryByIdAsync(categoryId, projectId);
        if (category == null) return NotFound();

        var ideas = await ideaService.GetIdeasByCategoryAsync(projectId, categoryId);
        ViewBag.Category = category;
        return View(ideas);
    }

    public async Task<IActionResult> Detail(int projectId, int categoryId, int ideaId)
    {

        ViewData["ShowModerationPopup"] = true;

        var idea = await ideaService.GetIdeaByIdAsync(projectId, categoryId, ideaId);
        if (idea == null) return NotFound();

        var comments = await ideaService.GetCommentsByIdeaAsync(projectId, categoryId, ideaId);
        var category = await categoryService.GetCategoryByIdAsync(categoryId, projectId);

        ViewBag.Category = category;
        ViewBag.ProjectId = projectId;
        ViewBag.CategoryId = categoryId;
        ViewBag.Comments = comments;
        ViewBag.Emojis = await emojiService.GetEmojisAsync();

        return View(idea);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIdea(
        int projectId,
        int categoryId,
        string description,
        string moderationStatus = "Unflagged")
    {
        // moderationStatus is a plain string ("Unflagged" or "Flagged") stamped by
        // the frontend JS. The UI layer passes it as-is to the service, which
        // forwards it to the API. The API/BL is responsible for parsing it to the enum.
        var success = await ideaService.CreateIdeaAsync(projectId, categoryId, description, moderationStatus);
        if (!success) return BadRequest("Failed to create idea");
        return RedirectToAction("Index", new { projectId, categoryId });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(
        int projectId,
        int categoryId,
        int ideaId,
        string text,
        string moderationStatus = "Unflagged")
    {
        var success = await commentService.AddCommentAsync(projectId, categoryId, ideaId, text, moderationStatus);
        if (!success) return BadRequest("Failed to add comment");
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> AddIconComment(int projectId, int categoryId, int ideaId, int emojiId, string returnPage)
    {
        await commentService.AddIconCommentAsync(projectId, categoryId, ideaId, emojiId);

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> AddCommentEmoji(int projectId, int categoryId, int ideaId, int commentId, int emojiId)
    {
        await commentService.AddCommentEmojiAsync(projectId, categoryId, ideaId, commentId, emojiId);
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveIconComment(int projectId, int categoryId, int ideaId, int emojiId, string returnPage)
    {
        await commentService.RemoveIconCommentAsync(projectId, categoryId, ideaId, emojiId);

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCommentEmoji(int projectId, int categoryId, int ideaId, int commentId, int emojiId)
    {
        await commentService.RemoveCommentEmojiAsync(projectId, categoryId, ideaId, commentId, emojiId);
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }
}