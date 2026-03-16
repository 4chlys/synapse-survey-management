using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Extensions;

public static class ControllerLayoutExtensions
{
    public static void SetLayoutData(this Controller controller, ProjectViewModel project,
        int projectId, bool showModerationPopup = false)
    {
        controller.ViewData["ProjectId"] = projectId;
        controller.ViewData["OrganisationLogoUrl"] = project?.OrganisationLogoUrl;
        controller.ViewData["OrganisationWebsiteUrl"] = project?.OrganisationWebsiteUrl;
        if (showModerationPopup)
            controller.ViewData["ShowModerationPopup"] = true;
    }
}
