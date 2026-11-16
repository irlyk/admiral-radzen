namespace Admiral.Playwright.Infrastructure.Browser;
// Playwright's built-in object converter expects mutable parameterless classes.
// Deserialize DOM snapshots through System.Text.Json so records keep immutable,
// PascalCase properties with explicit JSON names at the browser boundary.
internal static class DomSnapshots
{
    public static async Task<T> ReadSnapshotAsync<T>(this IPage page, string expression, object? argument = null)
    {
        var json = await page.EvaluateAsync<JsonElement>(expression, argument);
        return json.Deserialize<T>()!;
    }

    public static async Task<T> ReadSnapshotAsync<T>(this ILocator locator, string expression, object? argument = null)
    {
        var json = await locator.EvaluateAsync<JsonElement>(expression, argument);
        return json.Deserialize<T>()!;
    }

    public static async Task<T[]> ReadSnapshotsAsync<T>(this ILocator locator, string expression, object? argument = null)
    {
        var json = await locator.EvaluateAllAsync<JsonElement>(expression, argument);
        return json.Deserialize<T[]>()!;
    }
}
