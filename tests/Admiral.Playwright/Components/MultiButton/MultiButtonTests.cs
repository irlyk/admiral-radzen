using Admiral.Playwright.Pages.Components.MenuAction;

namespace Admiral.Playwright.Components.MultiButton;

[Category("Button")]
public sealed class MultiButtonTests : MenuActionTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task IndependentActionMenuKeyboardAndDisabledAsync(string theme, bool themeOnly) => VerifyKeyboardSelectionDisabledAsync(theme, "/components/multi-button", themeOnly);

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task DisablingAnOpenMenuClosesItAsync(string theme) => VerifyDisableFromOpenMenuAsync(theme, "/components/multi-button");

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task BothHalvesShareAppearanceAndDimensionAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/multi-button", themeOnly);
        await gallery.InitializeAsync();
        foreach (var (dimension, height) in new[] { ("S", 32), ("M", 40), ("L", 48), ("Xl", 56) })
        {
            await gallery.SelectAsync("Dimension", dimension);
            await Assertions.Expect(gallery.Main).ToHaveCSSAsync("height", height + "px");
            Assert.That((await gallery.Toggle.GetBoundsAsync()).Width, Is.EqualTo(height));
        }
        foreach (var appearance in new[] { "Primary", "Secondary", "Tertiary", "Ghost", "Danger", "Success", "White" })
        {
            await gallery.SelectAsync("Appearance", appearance);
            await Page.Mouse.MoveAsync(0, 0);
            var main = await gallery.ReadStyleAsync(gallery.Main);
            var toggle = await gallery.ReadStyleAsync(gallery.Toggle);
            Assert.That(toggle.Background, Is.EqualTo(main.Background), appearance);
            Assert.That(toggle.Color, Is.EqualTo(main.Color), appearance);
            await gallery.Toggle.HoverAsync();
            var hover = await gallery.ReadStyleAsync(gallery.Toggle);
            await gallery.Main.HoverAsync();
            Assert.That((await gallery.ReadStyleAsync(gallery.Main)).Background, Is.EqualTo(hover.Background), appearance + " hover");
        }
        await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{themeOnly}-appearance.png" });
    }

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task LoadingBlocksMainAndMenuAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/multi-button", themeOnly);
        await gallery.InitializeAsync();
        var before = await gallery.Widget.GetBoundsAsync();
        await gallery.SetAsync(themeOnly ? "IsBusy" : "Loading", true);
        await Assertions.Expect(gallery.Main).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Toggle).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Main).ToHaveAccessibleNameAsync("Actions");
        await gallery.Toggle.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        if (!themeOnly) Assert.That((await gallery.Widget.GetBoundsAsync()).Width, Is.EqualTo(before.Width).Within(1));
        Assert.That((await gallery.Widget.GetBoundsAsync()).Height, Is.EqualTo(before.Height).Within(1));
    }
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task PopupFitsSmallViewportAsync(string theme) => VerifySmallViewportAsync(theme, "/components/multi-button");
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task SkeletonBlocksActivationAndKeepsGeometryAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/multi-button");
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
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task DisabledMainActionDoesNotDisableMenuAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/multi-button");
        await gallery.InitializeAsync();
        await gallery.SetAsync("DisabledMainButton", true);
        await Assertions.Expect(gallery.Main).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Toggle).ToBeEnabledAsync();
        await gallery.Main.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Result).ToContainTextAsync("Main: 0");
        await gallery.Toggle.ClickAsync();
        await gallery.Popup.GetByRole(AriaRole.Menuitem, new() { Name = "Print", Exact = true }).ClickAsync();
        await Assertions.Expect(gallery.Result).ToContainTextAsync("Actions: 1 · Last: print");
    }
}
