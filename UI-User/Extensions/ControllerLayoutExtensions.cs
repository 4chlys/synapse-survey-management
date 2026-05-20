using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.UI.User.Helpers;
using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Extensions;

public static class ControllerLayoutExtensions
{
    public static void SetLayoutData(this Controller controller, ProjectViewModel project,
        int projectId, bool showModerationPopup = false)
    {
        controller.ViewData["ProjectId"] = projectId;
        controller.ViewData["ProjectName"] = project?.Name;
        controller.ViewData["OrganisationName"] = project?.OrganisationName;
        controller.ViewData["OrganisationLogoUrl"] = project?.OrganisationLogoUrl;
        controller.ViewData["OrganisationWebsiteUrl"] = project?.OrganisationWebsiteUrl;
        controller.ViewData["ProjectTheme"] = project?.Theme?.ToLowerInvariant() ?? "default";
        controller.ViewData["ProjectPrimaryColor"] = project?.PrimaryColor;
        controller.ViewData["ProjectPrimaryContent"] =
            !string.IsNullOrWhiteSpace(project?.PrimaryColor)
                ? ColorHelpers.ContrastingForeground(project!.PrimaryColor)
                : null;
        controller.ViewData["ProjectCustomCss"] = project?.CustomCss;
        if (showModerationPopup)
            controller.ViewData["ShowModerationPopup"] = true;
    }
}
