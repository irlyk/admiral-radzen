namespace Admiral.Playwright.Pages.Components.ImageViewer;

internal sealed class ImageViewerPage(IPage page) : GalleryPage(page)
{
    public ILocator Overlay => Page.Locator(".admiral-image-viewer__overlay");
    public ILocator Preview => Overlay.Locator(".admiral-image-viewer__preview");
    public ILocator Surface => Overlay.Locator(".admiral-image-viewer__surface");

    public async Task OpenAsync()
    {
        await NavigateAsync("/components/image-viewer");
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть: Горный пейзаж", Exact = true }).ClickAsync();
        await Preview.WaitForAsync();
        var bounds = await Overlay.GetBoundsAsync();
        var layoutBounds = await Page.Locator(".admiral-layout").GetBoundsAsync();
        var viewport = new
        {
            width = Page.ViewportSize!.Width,
            height = Page.ViewportSize!.Height
        };
        Assert.That(Math.Abs(bounds.X) < 1 && Math.Abs(bounds.Y) < 1, Is.True, "Dialog must start at viewport origin");
        Assert.That(
            Math.Abs(bounds.Width - layoutBounds.Width) < 1
                && Math.Abs(bounds.Height - viewport.height) < 1,
            Is.True,
            "Dialog must cover header/sidebar too");
        await Page.WaitForFunctionAsync("""
            () => {
              const image = document.querySelector('.admiral-image-viewer__preview');
              return (
                image?.complete &&
                image.naturalWidth > 0 &&
                !document.querySelector('.admiral-image-viewer__message')
              );
            }
            """);
    }

    public Task<double> GetScaleAsync() => Preview.EvaluateAsync<double>(
        "e => Number(e.style.transform.match(/scale\\(([^)]+)\\)/)?.[1])");

    public Task<IJSHandle> ExpectScaleAsync(double expected) => Page.WaitForFunctionAsync("""
        (expected) => {
          const value = Number(
            document
              .querySelector('.admiral-image-viewer__preview')
              ?.style.transform.match(/scale\(([^)]+)\)/)?.[1],
          );
          return Math.abs(value - expected) < 0.001;
        }
        """, expected);

    public async Task ZoomToMaximumAsync()
    {
        for (var i = 0; i < 12 && await GetScaleAsync() < 3; i++)
        {
            var previous = await GetScaleAsync();
            await Overlay.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true }).ClickAsync();
            await ExpectScaleAsync(Math.Min(3, previous + 0.25));
        }

        await ExpectScaleAsync(3);
    }
}
