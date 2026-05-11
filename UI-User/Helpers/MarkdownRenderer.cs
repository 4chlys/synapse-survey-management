using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.UI.User.Helpers;

/// <summary>
/// Converts a restricted Markdown subset (bold, italic, bullet/numbered lists) to safe HTML.
/// XSS-safe by construction: raw input is HTML-encoded before any pattern replacement, so
/// injected tags such as &lt;script&gt; are rendered as escaped text, never as markup.
/// </summary>
public static partial class MarkdownRenderer
{
    [GeneratedRegex(@"\*\*(.+?)\*\*", RegexOptions.Singleline)]
    private static partial Regex BoldPattern();

    [GeneratedRegex(@"\*(.+?)\*", RegexOptions.Singleline)]
    private static partial Regex ItalicPattern();

    [GeneratedRegex(@"^\d+\.\s+(.+)$")]
    private static partial Regex OrderedListPattern();

    public static string Render(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        var inUl = false;
        var inOl = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (string.IsNullOrWhiteSpace(line))
            {
                CloseList(sb, ref inUl, ref inOl);
                sb.Append("<br>");
                continue;
            }

            // Unordered list item: "- " or "* "
            if ((trimmed.StartsWith("- ") || trimmed.StartsWith("* ")) && trimmed.Length > 2)
            {
                CloseOl(sb, ref inOl);
                if (!inUl) { sb.Append("<ul>"); inUl = true; }
                sb.Append("<li>").Append(ApplyInline(WebUtility.HtmlEncode(trimmed[2..]))).Append("</li>");
                continue;
            }

            // Ordered list item: "1. " etc.
            var olMatch = OrderedListPattern().Match(trimmed);
            if (olMatch.Success)
            {
                CloseUl(sb, ref inUl);
                if (!inOl) { sb.Append("<ol>"); inOl = true; }
                sb.Append("<li>").Append(ApplyInline(WebUtility.HtmlEncode(olMatch.Groups[1].Value))).Append("</li>");
                continue;
            }

            // Regular paragraph line
            CloseList(sb, ref inUl, ref inOl);
            sb.Append("<p>").Append(ApplyInline(WebUtility.HtmlEncode(line))).Append("</p>");
        }

        CloseList(sb, ref inUl, ref inOl);
        return sb.ToString();
    }

    // Called only on already HTML-encoded text; the only < > we introduce are the tags we add here.
    private static string ApplyInline(string encoded)
    {
        var result = BoldPattern().Replace(encoded, "<strong>$1</strong>");
        result = ItalicPattern().Replace(result, "<em>$1</em>");
        return result;
    }

    private static void CloseUl(StringBuilder sb, ref bool inUl)
    {
        if (inUl) { sb.Append("</ul>"); inUl = false; }
    }

    private static void CloseOl(StringBuilder sb, ref bool inOl)
    {
        if (inOl) { sb.Append("</ol>"); inOl = false; }
    }

    private static void CloseList(StringBuilder sb, ref bool inUl, ref bool inOl)
    {
        CloseUl(sb, ref inUl);
        CloseOl(sb, ref inOl);
    }
}
