using Admiral.Playwright.Components.Button;

namespace Admiral.Playwright.Components.TextButton;

[Category("Button")]
public sealed class TextButtonStateTests : ButtonStateTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task FocusPointerKeyboardDisabledAndLoadingAsync(string theme, bool themeOnly) =>
        VerifyInteractionStatesAsync(theme, "/components/text-button", themeOnly);

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task LoadingPreservesGeometryAndAccessibleActionNameAsync(string theme) =>
        VerifyLoadingGeometryAndNameAsync(theme, "/components/text-button");
}
