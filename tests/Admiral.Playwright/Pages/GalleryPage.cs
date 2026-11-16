namespace Admiral.Playwright.Pages;

internal class GalleryPage(IPage page)
{
    protected IPage Page { get; } = page;
    protected IBrowserContext Context => Page.Context;
    protected static string Root => Repository.Root;
    protected static string BaseUrl => TestSettings.SmokeUrl;

    public async Task NavigateAsync(string route)
    {
        await Page.GotoAsync(BaseUrl.TrimEnd('/') + route, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();
    }

    public Task<string> GetActiveTextAsync()
    {
        return Page.EvaluateAsync<string>("() => document.activeElement?.textContent.trim()");
    }

    public Task SetToggleAsync(string name, bool isChecked = true)
    {
        return Page.GetByRole(AriaRole.Checkbox, new() { Name = name, Exact = true }).SetCheckedAsync(isChecked);
    }
}
