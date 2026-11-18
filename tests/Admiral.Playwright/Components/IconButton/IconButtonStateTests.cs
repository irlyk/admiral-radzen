using Admiral.Playwright.Components.Button;

namespace Admiral.Playwright.Components.IconButton;

[Category("Button")]
public sealed class IconButtonStateTests : ButtonStateTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task FocusPointerKeyboardDisabledAndLoadingAsync(string theme, bool themeOnly) =>
        VerifyInteractionStatesAsync(theme, "/components/icon-button", themeOnly);

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task LoadingPreservesGeometryAndAccessibleActionNameAsync(string theme) =>
        VerifyLoadingGeometryAndNameAsync(theme, "/components/icon-button");
}
