using System.Globalization;

namespace Admiral.Playwright.Infrastructure.Assertions;

internal static class ColorContrast
{
    public static double Ratio(string foreground, string background)
    {
        var foregroundLuminance = Luminance(foreground);
        var backgroundLuminance = Luminance(background);
        return (Math.Max(foregroundLuminance, backgroundLuminance) + 0.05) / (Math.Min(foregroundLuminance, backgroundLuminance) + 0.05);
    }

    private static double Luminance(string color)
    {
        var channels = Regex.Matches(color, @"[\d.]+")
            .Take(3)
            .Select(
                match => double.Parse(match.Value, CultureInfo.InvariantCulture) / 255)
            .Select(
                channel => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4))
            .ToArray();
        Assert.That(channels, Has.Length.EqualTo(3), $"Expected RGB color, received {color}");
        return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
    }
}
