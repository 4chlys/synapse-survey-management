namespace SynapseEnqueteManagement.BL.Helpers;

public static class ColorHelpers
{
    /// <summary>
    /// Returns the WCAG relative luminance of a #RRGGBB hex string in [0, 1].
    /// </summary>
    public static double RelativeLuminance(string hex)
    {
        var r = Convert.ToInt32(hex.Substring(1, 2), 16) / 255.0;
        var g = Convert.ToInt32(hex.Substring(3, 2), 16) / 255.0;
        var b = Convert.ToInt32(hex.Substring(5, 2), 16) / 255.0;

        static double Linearise(double c) =>
            c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        return 0.2126 * Linearise(r) + 0.7152 * Linearise(g) + 0.0722 * Linearise(b);
    }

    /// <summary>
    /// Returns "#000000" or "#FFFFFF" — whichever has better perceived contrast
    /// against the given #RRGGBB background. Threshold 0.45 is tuned for OKLCH primaries.
    /// </summary>
    public static string ContrastingForeground(string backgroundHex)
        => RelativeLuminance(backgroundHex) > 0.45 ? "#000000" : "#FFFFFF";

    /// <summary>
    /// Returns the WCAG contrast ratio between two #RRGGBB hex strings,
    /// in [1.0, 21.0]. Higher = more contrast.
    /// </summary>
    public static double ContrastRatio(string hex1, string hex2)
    {
        var l1 = RelativeLuminance(hex1);
        var l2 = RelativeLuminance(hex2);
        var lmax = Math.Max(l1, l2);
        var lmin = Math.Min(l1, l2);
        return (lmax + 0.05) / (lmin + 0.05);
    }
}
