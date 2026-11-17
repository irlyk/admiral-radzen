using Admiral.Playwright.Components.ButtonGroup;
using Admiral.Playwright.Pages.Components.ButtonGroup;

namespace Admiral.Playwright.Components.IconButtonGroup;

[Category("Button")]
public sealed class IconButtonGroupTests : ButtonGroupStateTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task DimensionsAppearanceBordersAndRtlAsync(string theme, bool themeOnly) =>
        VerifyGeometryAsync(theme, themeOnly, true, "/components/icon-button-group");

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task IndependentActionsFocusAndMixedStatesAsync(string theme, bool themeOnly) =>
        VerifyStatesAsync(theme, themeOnly, true, "/components/icon-button-group");

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task LargeGroupFitsNarrowViewportAsync(string theme, bool themeOnly) =>
        VerifyNarrowLayoutAsync(theme, themeOnly, "/components/icon-button-group");

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task ExplicitWidthDoesNotDistortIconButtonsAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonGroupPage(Page);
        await gallery.InitializeAsync("/components/icon-button-group", themeOnly);
        await gallery.Group.EvaluateAsync("e => e.style.width = '80px'");
        var geometry = await gallery.MeasureAsync();
        foreach (var button in geometry.Buttons)
        {
            Assert.That(button.Bounds.Width, Is.EqualTo(40).Within(1), "Constrained M icon remains square");
            Assert.That(button.Bounds.Height, Is.EqualTo(40).Within(1));
        }
    }
}
