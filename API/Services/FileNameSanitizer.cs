namespace SynapseEnqueteManagement.API.Services;

public static class FileNameSanitizer
{
    public static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim().Replace(' ', '-').ToLowerInvariant();
    }
}
