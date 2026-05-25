namespace SynapseEnqueteManagement.API.Helpers;

public static class AiSupportHelpers
{
    public static string FormatAgeRange(int? minAge, int? maxAge) => (minAge, maxAge) switch
    {
        (null, null)       => null,
        (var min, null)    => $"{min}+",
        (null, var max)    => $"0–{max}",
        (var min, var max) => $"{min}–{max}",
    };
}
