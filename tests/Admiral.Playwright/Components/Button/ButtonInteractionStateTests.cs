namespace Admiral.Playwright.Components.Button;

[Category("Button")]
public sealed class ButtonInteractionStateTests : ButtonStateTestBase
{
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public Task FocusPointerKeyboardDisabledAndLoadingAsync(string theme, bool themeOnly) =>
        VerifyInteractionStatesAsync(theme, "/components/button", themeOnly);

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public Task LoadingPreservesGeometryAndAccessibleActionNameAsync(string theme) =>
        VerifyLoadingGeometryAndNameAsync(theme, "/components/button");
}
