namespace SynapseEnqueteManagement.UI.Admin.Models.Enums;

// Local mirror of the API's ProjectTheme. Only used to populate the theme dropdown
// (the project theme is carried as a string across the HTTP boundary), so names
// must match the API enum.
public enum ProjectTheme
{
    Minimal,
    Friendly,
    Formal,
    HighContrast,
    Bold
}
