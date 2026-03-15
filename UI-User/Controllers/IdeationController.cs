using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Filters;
using SynapseEnqueteManagement.UI.User.Services.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Project;

namespace SynapseEnqueteManagement.UI.User.Controllers;

[ServiceFilter(typeof(RequiresSurveyCompletionFilter))]
public class IdeationController(
    CategoryService categoryService,
    IdeaService ideaService,
    CommentService commentService,
    EmojiService emojiService,
    IProjectApiService projectApiService,
    ParticipantService participantService)
    : Controller
{
    public async Task<IActionResult> Index(int projectId, int categoryId, bool contactInfo = false)
    {
        ViewData["ProjectId"] = projectId;
        ViewData["CategoryId"] = categoryId;
        ViewData["ShowModerationPopup"] = true;
        var project = await projectApiService.GetProjectAsync(projectId);
        ViewData["OrganisationLogoUrl"] = project?.OrganisationLogoUrl;
        ViewData["OrganisationWebsiteUrl"] = project?.OrganisationWebsiteUrl;
        if (contactInfo)
        {
            ViewData["ShowContactInfoPopup"] = true;
            ViewData["RequestContactInfo"] = project?.RequestContactInfo ?? false;
        }
        ViewBag.Emojis = await emojiService.GetEmojisAsync();

        var category = await categoryService.GetCategoryByIdAsync(categoryId, projectId);
        if (category == null) return NotFound();

        var ideas = await ideaService.GetIdeasByCategoryAsync(projectId, categoryId);
        ViewBag.Category = category;
        return View(ideas);
    }

    public async Task<IActionResult> Detail(int projectId, int categoryId, int ideaId)
    {
        var project = await projectApiService.GetProjectAsync(projectId);
        ViewData["OrganisationLogoUrl"] = project?.OrganisationLogoUrl;
        ViewData["OrganisationWebsiteUrl"] = project?.OrganisationWebsiteUrl;
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
        string moderationToken = null)
    {
        var result = await ideaService.CreateIdeaAsync(projectId, categoryId, description, moderationToken);
        if (!result.Success) return PartialView("_ModerationPopup");

        if (result.IsFirstIdea)
            return RedirectToAction("Index", new { projectId, categoryId, contactInfo = true });

        return RedirectToAction("Index", new { projectId, categoryId });
    }

    [HttpPost]
    public async Task<IActionResult> SaveContactInfo(int projectId, int categoryId, string email, string phoneNumber)
    {
        await participantService.SaveContactInfoAsync(projectId, email, phoneNumber);
        return RedirectToAction("Index", new { projectId, categoryId });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(
        int projectId,
        int categoryId,
        int ideaId,
        string text,
        string moderationToken = null)
    {
        var success = await commentService.AddCommentAsync(projectId, categoryId, ideaId, text, moderationToken);
        if (!success) return PartialView("_ModerationPopup");
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> AddIconComment(int projectId, int categoryId, int ideaId, int emojiId,
        string returnPage)
    {
        await commentService.AddIconCommentAsync(projectId, categoryId, ideaId, emojiId);

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> AddCommentEmoji(int projectId, int categoryId, int ideaId, int commentId,
        int emojiId)
    {
        await commentService.AddCommentEmojiAsync(projectId, categoryId, ideaId, commentId, emojiId);
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveIconComment(int projectId, int categoryId, int ideaId, int emojiId,
        string returnPage)
    {
        await commentService.RemoveIconCommentAsync(projectId, categoryId, ideaId, emojiId);

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCommentEmoji(int projectId, int categoryId, int ideaId, int commentId,
        int emojiId)
    {
        await commentService.RemoveCommentEmojiAsync(projectId, categoryId, ideaId, commentId, emojiId);
        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }
}