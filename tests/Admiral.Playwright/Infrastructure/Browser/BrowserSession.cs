namespace Admiral.Playwright.Infrastructure.Browser;

internal sealed class BrowserSession(IBrowserContext context, IPage page)
{
    private readonly List<string> _runtimeErrors = [];
    public IBrowserContext Context { get; } = context;
    public IPage Page { get; } = page;
    public IReadOnlyList<string> RuntimeErrors => _runtimeErrors;

    public static async Task<BrowserSession> CreateAsync(IBrowser browser, string theme, string culture, int width, int height, string url)
    {
        var context = await browser.NewContextAsync(
            new()
            {
                ViewportSize = new() { Width = width, Height = height },
                AcceptDownloads = true
            });
        try
        {
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
            await context.AddCookiesAsync(
                [new() { Name = "RadzenAdmiralTheme", Value = theme, Url = url }, new()
                {
                    Name = ".AspNetCore.Culture",
                    Value = $"c={culture}|uic={culture}",
                    Url = url
                }]);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(TestSettings.ElementTimeoutMs);
            var session = new BrowserSession(context, page);
            page.PageError += (_, error) => session._runtimeErrors.Add(error);
            return session;
        }
        catch
        {
            await context.CloseAsync();
            throw;
        }
    }

    public async Task<string[]> FindBlazorErrorsAsync()
    {
        var urls = new List<string>();
        foreach (var page in Context.Pages)
        {
            if (await page.Locator("#blazor-error-ui").IsVisibleAsync())
            {
                urls.Add(page.Url);
            }
        }

        return urls.ToArray();
    }
}
