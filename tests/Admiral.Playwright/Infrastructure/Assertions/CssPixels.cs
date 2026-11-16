using System.Globalization;

namespace Admiral.Playwright.Infrastructure.Assertions;

internal static class CssPixels
{
    public static double Parse(string value)
    {
        return double.Parse(
            value.EndsWith("px", StringComparison.Ordinal) ? value[..^2] : value,
            CultureInfo.InvariantCulture);
    }
}
