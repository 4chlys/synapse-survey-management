using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.UI.Admin.Filters;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

namespace SynapseEnqueteManagement.UI.Admin.Controllers;

[Authorize(Roles = "ProjectPlatformAdmin")]
[RequireProjectPlatformOwnership]
public class QuestionController(
    QuestionAdminService questionService,
    ProjectAdminService projectService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    // Question/{Index,Create,Edit} were legacy standalone MVC views that no
    // longer exist (the questionnaire builder lives entirely on Project/Questions).
    // They are removed here rather than ported to the new multilingual VM shape
    // to avoid maintaining dead code paths — only Delete is still wired from
    // the questionnaire-tree's per-card delete form.

    [HttpPost]
    public async Task<IActionResult> Delete(int projectId, int questionId, bool isNew = false)
    {
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project?.ProjectState is "Active" or "Archived")
        {
            TempData["Error"] = localizer["Project_LockedNotEditable"].Value;
            return RedirectToAction("Questions", "Project", new { projectId, isNew });
        }
        await questionService.DeleteQuestionAsync(projectId, questionId);
        return RedirectToAction("Questions", "Project", new { projectId, isNew });
    }
}
