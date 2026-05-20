namespace SynapseEnqueteManagement.UI.User.Helpers;

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
    /// Returns a #RRGGBB hex shifted by deltaL in OKLab lightness (range ~[0,1]).
    /// Negative values darken, positive values lighten. Used to derive hover / focus
    /// variants of a picked primary as a static fallback for browsers without
    /// relative-color OKLCH support.
    /// </summary>
    public static string ShiftLightness(string hex, double deltaL)
    {
        var (l, a, b) = HexToOkLab(hex);
        return OkLabToHex(Math.Clamp(l + deltaL, 0.0, 1.0), a, b);
    }

    /// <summary>
    /// Returns a #RRGGBB hex with the complementary hue (180° rotation in OKLab).
    /// Used to derive an accent companion to the picked primary.
    /// </summary>
    public static string ComplementaryHue(string hex)
    {
        var (l, a, b) = HexToOkLab(hex);
        return OkLabToHex(l, -a, -b);
    }

    private static (double L, double a, double b) HexToOkLab(string hex)
    {
        var rs = Convert.ToInt32(hex.Substring(1, 2), 16) / 255.0;
        var gs = Convert.ToInt32(hex.Substring(3, 2), 16) / 255.0;
        var bs = Convert.ToInt32(hex.Substring(5, 2), 16) / 255.0;

        var r = SrgbToLinear(rs);
        var g = SrgbToLinear(gs);
        var b = SrgbToLinear(bs);

        var lLms = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
        var mLms = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
        var sLms = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

        var l_ = Math.Cbrt(lLms);
        var m_ = Math.Cbrt(mLms);
        var s_ = Math.Cbrt(sLms);

        var L  = 0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_;
        var aa = 1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_;
        var bb = 0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_;
        return (L, aa, bb);
    }

    private static string OkLabToHex(double L, double a, double b)
    {
        var l_ = L + 0.3963377774 * a + 0.2158037573 * b;
        var m_ = L - 0.1055613458 * a - 0.0638541728 * b;
        var s_ = L - 0.0894841775 * a - 1.2914855480 * b;

        var lLms = l_ * l_ * l_;
        var mLms = m_ * m_ * m_;
        var sLms = s_ * s_ * s_;

        var r =  4.0767416621 * lLms - 3.3077115913 * mLms + 0.2309699292 * sLms;
        var g = -1.2684380046 * lLms + 2.6097574011 * mLms - 0.3413193965 * sLms;
        var bb = -0.0041960863 * lLms - 0.7034186147 * mLms + 1.7076147010 * sLms;

        var ri = (int)Math.Round(LinearToSrgb(Math.Clamp(r,  0.0, 1.0)) * 255);
        var gi = (int)Math.Round(LinearToSrgb(Math.Clamp(g,  0.0, 1.0)) * 255);
        var bi = (int)Math.Round(LinearToSrgb(Math.Clamp(bb, 0.0, 1.0)) * 255);
        return $"#{ri:X2}{gi:X2}{bi:X2}";
    }

    private static double SrgbToLinear(double c)
        => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double LinearToSrgb(double c)
        => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;
}
