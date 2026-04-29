using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Extensions;
using SynapseEnqueteManagement.UI.User.Filters;
using SynapseEnqueteManagement.UI.User.Models.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Project;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Controllers;

[ServiceFilter(typeof(RequiresSurveyCompletionFilter))]
public class IdeationController(
    CategoryService categoryService,
    IdeaService ideaService,
    CommentService commentService,
    EmojiService emojiService,
    IProjectApiService projectApiService,
    ParticipantService participantService,
    ISessionService sessionService)
    : Controller
{
    public async Task<IActionResult> Index(int projectId, int categoryId, bool contactInfo = false, string sort = "diverse")
    {
        var projectTask  = projectApiService.GetProjectAsync(projectId);
        var emojisTask   = emojiService.GetEmojisAsync(projectId);
        var categoryTask = categoryService.GetCategoryByIdAsync(categoryId, projectId);

        await Task.WhenAll(projectTask, emojisTask, categoryTask);

        var category = await categoryTask;
        if (category == null) return NotFound();

        var project = await projectTask;
        var replace = project?.ExtraIdeasReplaceExisting ?? false;
        var maxExtra = project?.MaxExtraIdeaRequests ?? 3;
        var used = sessionService.GetIdeaRefreshCount(projectId, categoryId);
        var remaining = Math.Max(0, maxExtra - used);
        var currentPage = used + 1;

        // Load the right ideas based on mode and refresh history
        var allIdeas = new List<IdeaViewModel>();
        string resolvedSort = sort;
        bool hasOwnIdeas = false;

        if (replace)
        {
            // Replace mode: only show the latest page
            var index = await ideaService.GetIdeasIndexAsync(projectId, categoryId, sort, currentPage);
            allIdeas.AddRange(index.Ideas);
            resolvedSort = index.ResolvedSort;
            hasOwnIdeas = index.HasOwnIdeas;
        }
        else
        {
            // Supplement mode: show all pages up to current
            var pageTasks = Enumerable.Range(1, currentPage)
                .Select(p => ideaService.GetIdeasIndexAsync(projectId, categoryId, sort, p))
                .ToList();
            var results = await Task.WhenAll(pageTasks);
            foreach (var result in results)
                allIdeas.AddRange(result.Ideas);
            resolvedSort = results[0].ResolvedSort;
            hasOwnIdeas = results.Any(r => r.HasOwnIdeas);
        }

        this.SetLayoutData(project, projectId, showModerationPopup: true);

        return View(new IdeaIndexViewModel
        {
            ProjectId            = projectId,
            CategoryId           = categoryId,
            Category             = category,
            Ideas                = allIdeas,
            Emojis               = await emojisTask,
            ShowContactInfoPopup = contactInfo && (project?.RequestContactInfo ?? false),
            CurrentSort          = resolvedSort,
            HasOwnIdeas          = hasOwnIdeas,
            ExtraIdeasReplaceExisting = replace,
            MaxExtraIdeaRequests      = maxExtra,
            RemainingExtraRequests    = remaining
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

    [HttpGet]
    public async Task<IActionResult> LoadIdeas(int projectId, int categoryId, int page = 1, string sort = "diverse")
    {
        var project = await projectApiService.GetProjectAsync(projectId);
        var maxExtra = project?.MaxExtraIdeaRequests ?? 3;
        var used = sessionService.GetIdeaRefreshCount(projectId, categoryId);

        if (used >= maxExtra)
            return Json(new { ideas = Array.Empty<object>(), remaining = 0 });

        sessionService.IncrementIdeaRefreshCount(projectId, categoryId);
        var remaining = maxExtra - used - 1;

        var index = await ideaService.GetIdeasIndexAsync(projectId, categoryId, sort, page);
        return Json(new { ideas = index.Ideas, remaining });
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
}
