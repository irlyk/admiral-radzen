using Admiral.Playwright.Components.MultiButton;
using Admiral.Playwright.Pages.Components.MenuAction;

namespace Admiral.Playwright.Components.TextButtonMenu;

[Category("Button")]
public sealed class TextButtonMenuTests : MenuActionTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task KeyboardSelectionAndDisabledAsync(string theme, bool themeOnly) => VerifyKeyboardSelectionDisabledAsync(theme, "/components/text-button-menu", themeOnly);

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task DisablingAnOpenMenuClosesItAsync(string theme) => VerifyDisableFromOpenMenuAsync(theme, "/components/text-button-menu");

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task ChevronReflectsExpandedStateAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/text-button-menu");
        await gallery.InitializeAsync();
        var arrow = gallery.Toggle.Locator(".admiral-icon").Last;
        var closed = await arrow.GetCssAsync("transform");
        await gallery.Toggle.ClickAsync();
        await Assertions.Expect(gallery.Toggle).ToHaveAttributeAsync("aria-expanded", "true");
        Assert.That(await arrow.GetCssAsync("transform"), Is.Not.EqualTo(closed));
        await gallery.CloseOutsideAsync();
        await Assertions.Expect(arrow).ToHaveCSSAsync("transform", closed);
    }
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task PopupFitsSmallViewportAsync(string theme) => VerifySmallViewportAsync(theme, "/components/text-button-menu");
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task SkeletonBlocksActivationAndKeepsGeometryAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/text-button-menu");
        await gallery.InitializeAsync();
        var before = await gallery.Widget.GetBoundsAsync();
        await gallery.SetAsync("Skeleton", true);
        await Assertions.Expect(gallery.Main).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Toggle).ToBeDisabledAsync();
        await gallery.Toggle.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        Assert.That((await gallery.Widget.GetBoundsAsync()).Width, Is.EqualTo(before.Width).Within(1));
        await gallery.SetAsync("Skeleton", false);
        await gallery.Toggle.ClickAsync();
        await Assertions.Expect(gallery.Popup).ToBeVisibleAsync();
    }
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task DimensionsPaletteAndLoadingAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/text-button-menu", themeOnly);
        await gallery.InitializeAsync();
        foreach (var dimension in new[] { "S", "M" })
        {
            await gallery.SelectAsync("Dimension", dimension);
            await Assertions.Expect(gallery.Main).ToHaveCSSAsync("height", themeOnly ? dimension == "S" ? "32px" : "40px" : dimension == "S" ? "20px" : "24px");
        }
        foreach (var appearance in new[] { "Primary", "Secondary" })
        {
            await gallery.SelectAsync("Appearance", appearance);
            await Page.Mouse.MoveAsync(0, 0);
            var normal = await gallery.ReadStyleAsync(gallery.Main);
            await gallery.Main.HoverAsync();
            Assert.That(await gallery.ReadStyleAsync(gallery.Main), Is.Not.EqualTo(normal));
        }
        var before = await gallery.Widget.GetBoundsAsync();
        await gallery.SetAsync(themeOnly ? "IsBusy" : "Loading", true);
        await Assertions.Expect(gallery.Main).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Toggle).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Main).ToHaveAccessibleNameAsync("Actions");
        await gallery.Toggle.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        if (!themeOnly) Assert.That((await gallery.Widget.GetBoundsAsync()).Width, Is.EqualTo(before.Width).Within(1));
        await gallery.SetAsync(themeOnly ? "IsBusy" : "Loading", false);
        await gallery.Toggle.ClickAsync();
        await Assertions.Expect(gallery.Popup).ToBeVisibleAsync();
    }
}
