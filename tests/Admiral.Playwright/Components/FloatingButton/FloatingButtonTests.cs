using Admiral.Playwright.Pages.Components.FloatingButton;

namespace Admiral.Playwright.Components.FloatingButton;

[Category("Button")]
public sealed class FloatingButtonTests : GalleryTest
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task GeometryPaletteKeyboardAndDisabledAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new FloatingButtonPage(Page);
        await gallery.InitializeAsync(themeOnly);
        foreach (var size in new[] { "Medium", "Xl" })
        {
            await gallery.SelectAsync("Size", size);
            await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex(size == "Xl" ? "--xl" : "--m"));
            var bounds = await gallery.Button.GetBoundsAsync();
            Assert.That(bounds.Width, Is.EqualTo(size == "Xl" ? 56 : 40));
            Assert.That(bounds.Height, Is.EqualTo(bounds.Width));
            Assert.That((await gallery.Content.GetBoundsAsync()).Width, Is.EqualTo(bounds.Width));
        }
        foreach (var appearance in new[] { "Primary", "Secondary" })
        {
            await gallery.SelectAsync("Appearance", appearance);
            await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex("--" + appearance.ToLowerInvariant()));
            await Page.Mouse.MoveAsync(0, 0);
            var normal = await gallery.Content.GetCssAsync("background-color");
            await gallery.Button.HoverAsync();
            var hover = await gallery.Content.GetCssAsync("background-color");
            Assert.That(hover, Is.Not.EqualTo(normal));
            await Page.Mouse.DownAsync();
            Assert.That(await gallery.Content.GetCssAsync("background-color"), Is.Not.EqualTo(hover));
            await Assertions.Expect(gallery.Button).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
            await Page.Mouse.UpAsync();
        }
        await Page.Keyboard.PressAsync("Tab");
        await gallery.Button.FocusAsync();
        await Assertions.Expect(gallery.Button).ToHaveCSSAsync("outline-width", "2px");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*4$"));
        await gallery.SetAsync("Disabled", true);
        await Assertions.Expect(gallery.Button).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Content).ToHaveCSSAsync("color", theme == "admiral" ? "rgb(178, 181, 184)" : "rgb(81, 83, 87)");
        await Assertions.Expect(gallery.Button.Locator(".admiral-floating-button__badge")).ToHaveCountAsync(0);
        await gallery.Button.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*4$"));
        await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{themeOnly}-disabled.png" });
    }

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task MobileOffsetAffectsOnlyFixedPositionAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en", 390, 800);
        var gallery = new FloatingButtonPage(Page);
        await gallery.InitializeAsync(themeOnly);
        await gallery.SetAsync("Mobile", true);
        await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex("--mobile"));
        await Assertions.Expect(gallery.Button).ToHaveCSSAsync("right", "0px");
        await Assertions.Expect(gallery.Button).ToHaveCSSAsync("bottom", "0px");
        await gallery.SetAsync("Fixed", true);
        await Assertions.Expect(gallery.Button).ToHaveCSSAsync("position", "fixed");
        var mobile = await gallery.Button.GetBoundsAsync();
        Assert.That(390 - mobile.X - mobile.Width, Is.EqualTo(16).Within(1));
        Assert.That(800 - mobile.Y - mobile.Height, Is.EqualTo(16).Within(1));
        await Page.Locator(".admiral-layout__body").EvaluateAsync("e => e.scrollTop = 0");
        var scrolled = await gallery.Button.GetBoundsAsync();
        Assert.That(scrolled.X, Is.EqualTo(mobile.X).Within(1));
        Assert.That(scrolled.Y, Is.EqualTo(mobile.Y).Within(1));
        await Page.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{themeOnly}-mobile-fixed.png" });
        await gallery.SetAsync("Mobile", false);
        await Assertions.Expect(gallery.Button).ToHaveCSSAsync("right", "28px");
        await Assertions.Expect(gallery.Button).ToHaveCSSAsync("bottom", "28px");
    }

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task IconReplacementAndBadgePriorityAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new FloatingButtonPage(Page);
        await gallery.InitializeAsync(themeOnly);
        var before = await gallery.Button.Locator("svg").ScreenshotAsync();
        await gallery.SelectAsync("Icon", "admiral:system/print-outline");
        await Assertions.Expect(gallery.Button.Locator("[data-admiral-icon-loaded='system/print-outline'] svg")).ToBeVisibleAsync();
        var after = await gallery.Button.Locator("svg").ScreenshotAsync();
        Assert.That((await ScreenshotComparer.CompareAsync(Page, before, after))?.Changed, Is.GreaterThan(10));
        await gallery.SelectAsync("Icon", "refresh");
        await Assertions.Expect(gallery.Button.Locator(".rzi")).ToHaveTextAsync("refresh");
        await Assertions.Expect(gallery.Button.Locator(".rzi")).Not.ToHaveClassAsync(new Regex("rzi-admiral"));
        await Assertions.Expect(gallery.Button.Locator("svg")).ToHaveCountAsync(0);
        await gallery.SelectAsync("Icon", "admiral:system/email-outline");
        await Assertions.Expect(gallery.Button.Locator("[data-admiral-icon-loaded='system/email-outline'] svg")).ToBeVisibleAsync();
        await gallery.SetAsync("BadgeDot", true);
        await Assertions.Expect(gallery.Button.Locator(".admiral-floating-button__badge--dot")).ToHaveTextAsync("");
        await Assertions.Expect(gallery.Button).ToHaveAccessibleNameAsync("Compose");
    }
}
