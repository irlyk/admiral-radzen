namespace Admiral.Playwright.Infrastructure.Browser;

internal static class LocatorCssExtensions
{
    public static Task<string> GetCssAsync(this ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>(
            "(element, property) => getComputedStyle(element).getPropertyValue(property)",
            property);
    }
}
