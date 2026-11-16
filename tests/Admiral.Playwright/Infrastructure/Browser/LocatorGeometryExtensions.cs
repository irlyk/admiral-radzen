namespace Admiral.Playwright.Infrastructure.Browser;

internal static class LocatorGeometryExtensions
{
    public static async Task<ElementBounds> GetBoundsAsync(this ILocator locator)
    {
        var bounds = await locator.BoundingBoxAsync() ?? throw new AssertionException("Element has no bounding box");
        return new ElementBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }
}
