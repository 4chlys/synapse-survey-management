using System.Text.RegularExpressions;
using SynapseEnqueteManagement.BL.Exceptions;

namespace SynapseEnqueteManagement.BL.Helpers;

// Theme/appearance validation shared by ProjectManager.CreateProjectAsync and UpdateProjectAsync.
// Extracted to remove the duplicated colour/CSS/age checks that previously lived inline in both.
public static class ProjectThemeValidator
{
    // Base backgrounds the primary colour must stay legible against (light surface + dark surface).
    private const string LightBase = "#FFFFFF";
    private const string DarkBase = "#1A1430"; // approx of oklch(15% 0.04 278)

    public static void ValidatePrimaryColor(string primaryColor)
    {
        if (string.IsNullOrEmpty(primaryColor)) return;

        if (!Regex.IsMatch(primaryColor, @"^#[0-9A-Fa-f]{6}$"))
            throw new LocalizedValidationException("Validation.PrimaryColor.Invalid");

        if (ColorHelpers.ContrastRatio(primaryColor, LightBase) < 3.0
         || ColorHelpers.ContrastRatio(primaryColor, DarkBase) < 3.0)
            throw new LocalizedValidationException("Validation.PrimaryColor.LowContrast");
    }

    public static void ValidateCustomCss(string customCss)
    {
        if (!string.IsNullOrEmpty(customCss) &&
            Regex.IsMatch(customCss,
                @"<\s*/?\s*(script|style)\b|expression\s*\(|url\s*\(\s*['""]?\s*javascript:|@import\b|behavior\s*:",
                RegexOptions.IgnoreCase))
            throw new LocalizedValidationException("Validation.CustomCss.Disallowed");
    }

    public static void ValidateAgeRange(int? minAge, int? maxAge)
    {
        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            throw new LocalizedValidationException("Validation.MinAge.GreaterThanMaxAge");
    }
}
