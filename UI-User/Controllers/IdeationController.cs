using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.UI.User.Extensions;
using SynapseEnqueteManagement.UI.User.Filters;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
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
    ParticipantService participantService,
    IWebHostEnvironment env,
    IConfiguration configuration,
    ILogger<IdeationController> logger)
    : Controller
{
    public async Task<IActionResult> Index(int projectId, int categoryId, bool contactInfo = false, string sort = "diverse")
    {
        var projectTask  = projectApiService.GetProjectAsync(projectId);
        var emojisTask   = emojiService.GetEmojisAsync(projectId);
        var categoryTask = categoryService.GetCategoryByIdAsync(categoryId, projectId);
        var indexTask    = ideaService.GetIdeasIndexAsync(projectId, categoryId, sort);

        await Task.WhenAll(projectTask, emojisTask, categoryTask, indexTask);

        var category = await categoryTask;
        if (category == null) return NotFound();

        var project = await projectTask;
        var index   = await indexTask;

        this.SetLayoutData(project, projectId, showModerationPopup: true);

        return View(new IdeaIndexViewModel
        {
            ProjectId            = projectId,
            CategoryId           = categoryId,
            Category             = category,
            Ideas                = index.Ideas,
            Emojis               = await emojisTask,
            ShowContactInfoPopup = contactInfo && (project?.RequestContactInfo ?? false),
            CurrentSort          = index.ResolvedSort,
            HasOwnIdeas          = index.HasOwnIdeas
        });
    }

    public async Task<IActionResult> Detail(int projectId, int categoryId, int ideaId)
    {
        var projectTask = projectApiService.GetProjectAsync(projectId);
        var ideaTask = ideaService.GetIdeaByIdAsync(projectId, categoryId, ideaId);
        var commentsTask = ideaService.GetCommentsByIdeaAsync(projectId, categoryId, ideaId);
        var categoryTask = categoryService.GetCategoryByIdAsync(categoryId, projectId);
        var emojisTask = emojiService.GetEmojisAsync(projectId);

        await Task.WhenAll(projectTask, ideaTask, commentsTask, categoryTask, emojisTask);

        var idea = await ideaTask;
        if (idea == null) return NotFound();

        this.SetLayoutData(await projectTask, projectId, showModerationPopup: true);

        return View(new IdeaDetailViewModel
        {
            ProjectId = projectId,
            CategoryId = categoryId,
            Category = (await categoryTask)!,
            Idea = idea,
            Comments = await commentsTask,
            Emojis = await emojisTask
        });
    }

    public async Task<IActionResult> MyIdeas(int projectId, int categoryId)
    {
        var projectTask = projectApiService.GetProjectAsync(projectId);
        var categoryTask = categoryService.GetCategoryByIdAsync(categoryId, projectId);
        var ideasTask = ideaService.GetMyIdeasAsync(projectId, categoryId);

        await Task.WhenAll(projectTask, categoryTask, ideasTask);

        var category = await categoryTask;
        if (category == null) return NotFound();

        this.SetLayoutData(await projectTask, projectId);

        return View(new MyIdeasViewModel
        {
            ProjectId = projectId,
            CategoryId = categoryId,
            Category = category,
            Ideas = await ideasTask
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateIdea(
        int projectId,
        int categoryId,
        string description,
        string moderationToken = null,
        string imageUrl = null,
        string imageModerationToken = null)
    {
        if (!string.IsNullOrEmpty(imageUrl) && !IsAllowedImageUrl(imageUrl))
        {
            logger.LogWarning("Rejected imageUrl {Url} for project {ProjectId} — fails origin check", imageUrl, projectId);
            imageUrl = null;
            imageModerationToken = null;
        }

        var result = await ideaService.CreateIdeaAsync(projectId, categoryId, description, moderationToken,
            imageUrl, imageModerationToken);
        if (!result.Success) return PartialView("_ModerationPopup");

        if (result.ShouldAskForContactInfo)
            return RedirectToAction("Index", new { projectId, categoryId, contactInfo = true });

        return RedirectToAction("Index", new { projectId, categoryId });
    }

    [HttpPost]
    public async Task<IActionResult> SaveContactInfo(int projectId, int categoryId, string email)
    {
        await participantService.SaveContactInfoAsync(projectId, email);
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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var idea = await ideaService.GetIdeaByIdAsync(projectId, categoryId, ideaId);
            return Json(idea?.Emojis ?? []);
        }

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> AddCommentEmoji(int projectId, int categoryId, int ideaId, int commentId,
        int emojiId)
    {
        await commentService.AddCommentEmojiAsync(projectId, categoryId, ideaId, commentId, emojiId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var comments = await ideaService.GetCommentsByIdeaAsync(projectId, categoryId, ideaId);
            var comment = comments.FirstOrDefault(c => c.CommentId == commentId);
            return Json(comment?.Emojis ?? []);
        }

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveIconComment(int projectId, int categoryId, int ideaId, int emojiId,
        string returnPage)
    {
        await commentService.RemoveIconCommentAsync(projectId, categoryId, ideaId, emojiId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var idea = await ideaService.GetIdeaByIdAsync(projectId, categoryId, ideaId);
            return Json(idea?.Emojis ?? []);
        }

        if (returnPage == "Index")
            return RedirectToAction("Index", new { projectId, categoryId });

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCommentEmoji(int projectId, int categoryId, int ideaId, int commentId,
        int emojiId)
    {
        await commentService.RemoveCommentEmojiAsync(projectId, categoryId, ideaId, commentId, emojiId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var comments = await ideaService.GetCommentsByIdeaAsync(projectId, categoryId, ideaId);
            var comment = comments.FirstOrDefault(c => c.CommentId == commentId);
            return Json(comment?.Emojis ?? []);
        }

        return RedirectToAction("Detail", new { projectId, categoryId, ideaId });
    }

    private bool IsAllowedImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        if (env.IsDevelopment())
        {
            var apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5159";
            if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)) return false;
            return (uri.Scheme == "https" || uri.Scheme == "http")
                && string.Equals(uri.Host, apiUri.Host, StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.StartsWith("/uploads/tmp/ideas/", StringComparison.OrdinalIgnoreCase);
        }

        var bucket = configuration["GcsBucket"]
                     ?? Environment.GetEnvironmentVariable("GCS_BUCKET")
                     ?? "synapse-assets-prod";
        return uri.Scheme == "https"
            && string.Equals(uri.Host, "storage.googleapis.com", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.StartsWith($"/{bucket}/tmp/ideas/", StringComparison.OrdinalIgnoreCase);
    }
}
