using System.Globalization;

namespace Admiral.Playwright.Infrastructure.Assertions;

internal static class NumericValue
{
    public static double ParseInvariant(string? value)
    {
        Assert.That(value, Is.Not.Null.And.Not.Empty, "Expected a numeric DOM value");
        return double.Parse(value!, CultureInfo.InvariantCulture);
    }
}
