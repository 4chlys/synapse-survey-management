using System.Net;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.Helpers;

public static class HtmlHelpers
{
    private static readonly Regex TagPattern      = new("<[^>]+>",  RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s{2,}", RegexOptions.Compiled);

    /// <summary>
    /// Strips HTML tags, decodes HTML entities (&amp;amp; → &amp;, &amp;lt; → &lt;, etc.)
    /// and collapses redundant whitespace. Safe to call on already-stripped text.
    /// </summary>
    public static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return html ?? "";
        return WebUtility.HtmlDecode(
            WhitespacePattern.Replace(TagPattern.Replace(html, " "), " ").Trim());
    }
}
