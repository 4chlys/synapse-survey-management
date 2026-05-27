namespace SynapseEnqueteManagement.API.Services;

// Turns an arbitrary display name into a safe, lowercase, dash-separated file-name stem.
// Shared by the export controllers so the rule lives in one place.
public static class FileNameSanitizer
{
    public static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim().Replace(' ', '-').ToLowerInvariant();
    }
}
