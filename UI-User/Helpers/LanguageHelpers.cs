namespace SynapseEnqueteManagement.UI.User.Helpers;

public static class LanguageHelpers
{
    public static string DisplayName(string languageCode) => languageCode switch
    {
        "nl" => "Nederlands",
        "en" => "English",
        _ => languageCode
    };
}