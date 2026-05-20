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
        // Static hex fallbacks for browsers without relative-color OKLCH support.
        // Hover -6% OKLab L, focus -12% OKLab L, accent = 180° complementary hue.
        if (!string.IsNullOrWhiteSpace(project?.PrimaryColor))
        {
            controller.ViewData["ProjectPrimaryHover"] = ColorHelpers.ShiftLightness(project!.PrimaryColor, -0.06);
            controller.ViewData["ProjectPrimaryFocus"] = ColorHelpers.ShiftLightness(project!.PrimaryColor, -0.12);
            controller.ViewData["ProjectAccent"]       = ColorHelpers.ComplementaryHue(project!.PrimaryColor);
        }
        controller.ViewData["ProjectCustomCss"] = project?.CustomCss;
        if (showModerationPopup)
            controller.ViewData["ShowModerationPopup"] = true;
    }
}
