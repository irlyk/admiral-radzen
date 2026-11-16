namespace Admiral.Playwright.Infrastructure.Browser;

public abstract class GalleryTest : BrowserTest
{
    protected IPage Page { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;

    protected async Task StartPageAsync(string theme, string culture = "ru", int width = 1440, int height = 1000)
    {
        (Context, Page) = await NewPageAsync(theme, culture, width, height);
    }
}
