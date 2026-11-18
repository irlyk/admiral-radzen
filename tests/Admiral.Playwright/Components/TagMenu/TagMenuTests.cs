using Admiral.Playwright.Components.MultiButton;
using Admiral.Playwright.Pages.Components.MenuAction;

namespace Admiral.Playwright.Components.TagMenu;

[Category("Button")]
public sealed class TagMenuTests : MenuActionTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task KeyboardSelectionAndDisabledAsync(string theme, bool themeOnly) => VerifyKeyboardSelectionDisabledAsync(theme, "/components/tag-menu", themeOnly);

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task DisablingAnOpenMenuClosesItAsync(string theme) => VerifyDisableFromOpenMenuAsync(theme, "/components/tag-menu");

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task StatusAndChevronFitBothTagSizesAsync(string theme)
    {
        await StartPageAsync(theme, "en", 390, 800);
        var gallery = new MenuActionPage(Page, "/components/tag-menu");
        await gallery.InitializeAsync();
        foreach (var (dimension, height) in new[] { ("S", 20), ("M", 24) })
        {
            await gallery.SelectAsync("Dimension", dimension);
            await Assertions.Expect(gallery.Main).ToHaveCSSAsync("height", height + "px");
            var arrow = gallery.Main.Locator("[data-admiral-icon-loaded='system/chevron-down-outline']");
            await Assertions.Expect(arrow.Locator("svg")).ToBeVisibleAsync();
            Assert.That((await arrow.GetBoundsAsync()).Width, Is.EqualTo(16));
            var closed = await arrow.GetCssAsync("transform");
            await gallery.Toggle.ClickAsync();
            await Assertions.Expect(gallery.Toggle).ToHaveAttributeAsync("aria-expanded", "true");
            Assert.That(await arrow.GetCssAsync("transform"), Is.Not.EqualTo(closed));
            await gallery.CloseOutsideAsync();
        }
        foreach (var kind in new[] { "Neutral", "Primary", "Success", "Danger", "Warning" })
        {
            await gallery.SelectAsync("Kind", kind);
            await gallery.SetAsync("StatusViaBackground", true);
            var normal = await gallery.ReadStyleAsync(gallery.Main);
            if (kind == "Neutral")
            {
                Assert.That(normal.Color, Is.EqualTo(theme == "admiral" ? "rgb(37, 38, 41)" : "rgb(227, 229, 231)"));
            }
            else
            {
                Assert.That(ColorContrast.Ratio(normal.Color, normal.Background), Is.GreaterThan(4.5), kind);
            }

            await gallery.Main.HoverAsync();
            Assert.That((await gallery.ReadStyleAsync(gallery.Main)).Background, Is.Not.EqualTo(normal.Background));
            await Page.Mouse.MoveAsync(0, 0);
        }
        await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-tag-status.png" });
    }
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task PopupFitsSmallViewportAsync(string theme) => VerifySmallViewportAsync(theme, "/components/tag-menu");
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task StatusBackgroundHoverKeepsItsKindAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, "/components/tag-menu");
        await gallery.InitializeAsync();
        await gallery.SetAsync("StatusViaBackground", true);
        foreach (var (kind, light, dark) in new[] {
            ("Primary", "rgb(221, 233, 255)", "rgb(22, 43, 103)"),
            ("Success", "rgb(195, 244, 211)", "rgb(14, 59, 30)"),
            ("Danger", "rgb(255, 221, 221)", "rgb(87, 27, 27)"),
            ("Warning", "rgb(255, 222, 206)", "rgb(89, 28, 4)") })
        {
            await gallery.SelectAsync("Kind", kind);
            await gallery.Main.HoverAsync();
            await Assertions.Expect(gallery.Main).ToHaveCSSAsync("background-color", theme == "admiral" ? light : dark);
        }
    }
}
