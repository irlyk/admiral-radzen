namespace Admiral.Playwright.Infrastructure.Browser;

[NonParallelizable]
public abstract class BrowserTest
{
    private readonly List<BrowserSession> _sessions = [];
    private readonly List<IAsyncDisposable> _resources = [];
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    protected static string Root => Repository.Root;
    protected static string BaseUrl => TestSettings.SmokeUrl;
    protected string Output => Path.Combine(TestSettings.ArtifactRoot, GetType().Name);

    // Hosts must outlive their pages, including background WASM downloads.
    protected T Own<T>(T resource)
        where T : IAsyncDisposable
    {
        _resources.Add(resource);
        return resource;
    }

    [OneTimeSetUp]
    public async Task LaunchBrowserAsync()
    {
        Directory.CreateDirectory(Output);
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(
            new()
            {
                Headless = TestSettings.Headless,
                ExecutablePath = TestSettings.ChromiumPath
            });
    }

    protected async Task<(IBrowserContext Context, IPage Page)> NewPageAsync(
        string theme = "admiral",
        string culture = "ru",
        int width = 1440,
        int height = 1000,
        string? url = null)
    {
        var session = await BrowserSession.CreateAsync(_browser, theme, culture, width, height, url ?? BaseUrl);
        _sessions.Add(session);
        return (session.Context, session.Page);
    }

    [TearDown]
    public async Task CloseContextsAsync()
    {
        var failed = TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed;
        var overlays = new List<string>();
        var errors = new List<string>();
        var artifacts = new FailureArtifacts(Output);
        try
        {
            foreach (var (session, index) in _sessions.Select((session, index) => (session, index)))
            {
                overlays.AddRange(await session.FindBlazorErrorsAsync());
                errors.AddRange(session.RuntimeErrors);
                failed |= overlays.Count > 0 || errors.Count > 0;
                if (failed && session.Context.Pages.Count > 0)
                {
                    await artifacts.SaveScreenshotAsync(session.Context.Pages[0], index);
                }

                var trace = failed ? artifacts.GetTracePath(index) : null;
                await session.Context.Tracing.StopAsync(new() { Path = trace });
                if (trace is not null)
                {
                    TestContext.AddTestAttachment(trace);
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(errors, Is.Empty, "Browser runtime errors");
                Assert.That(overlays, Is.Empty, "Blazor error overlay");
            }
        }
        finally
        {
            try
            {
                foreach (var session in _sessions)
                {
                    await session.Context.CloseAsync();
                }
            }
            finally
            {
                _sessions.Clear();
                foreach (var resource in Enumerable.Reverse(_resources))
                {
                    await resource.DisposeAsync();
                }

                _resources.Clear();
            }
        }
    }

    [OneTimeTearDown]
    public async Task CloseBrowserAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
