namespace Admiral.Playwright.Components.ButtonGroup;

[Category("Button")]
public sealed class ButtonGroupTests : ButtonGroupStateTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task DimensionsAppearanceBordersAndRtlAsync(string theme, bool themeOnly) =>
        VerifyGeometryAsync(theme, themeOnly, false, "/components/button-groups");

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task IndependentActionsFocusAndMixedStatesAsync(string theme, bool themeOnly) =>
        VerifyStatesAsync(theme, themeOnly, false, "/components/button-groups");

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task LargeGroupFitsNarrowViewportAsync(string theme, bool themeOnly) =>
        VerifyNarrowLayoutAsync(theme, themeOnly, "/components/button-groups");

}
