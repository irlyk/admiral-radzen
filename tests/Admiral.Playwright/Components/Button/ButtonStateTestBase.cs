using Admiral.Playwright.Pages.Components.Button;

namespace Admiral.Playwright.Components.Button;

// Shared UI contracts; each fixture binds them to its own component route.
public abstract class ButtonStateTestBase : GalleryTest
{
    protected async Task VerifyInteractionStatesAsync(string theme, string route, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonPage(Page);
        await gallery.InitializeAsync(route, themeOnly);
        var button = gallery.Button;
        await Page.Mouse.MoveAsync(0, 0);
        var normal = await gallery.ReadStateAsync();
        var name = await ButtonPage.ReadActionNameAsync(button);
        var file = $"{Output}/{theme}-{route.Split('/').Last()}-{themeOnly}";
        var normalPixels = await button.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled, Path = file + "-normal.png" });
        await button.HoverAsync();
        Assert.That(await gallery.ReadStateAsync(), Is.Not.EqualTo(normal), "Enabled hover is visible");
        await Page.Mouse.DownAsync();
        Assert.That(await gallery.ReadStateAsync(), Is.Not.EqualTo(normal), "Pressed differs from normal");
        await Page.Mouse.UpAsync();
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*1$"));

        await Page.Keyboard.PressAsync("Tab");
        await button.FocusAsync();
        await Assertions.Expect(button).ToHaveCSSAsync("outline-style", "solid");
        await Assertions.Expect(button).ToHaveCSSAsync("outline-width", "2px");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*3$"));

        await gallery.SetStateAsync("Disabled", true);
        await Assertions.Expect(button).ToBeDisabledAsync();
        await Page.Mouse.MoveAsync(0, 0);
        var disabled = await gallery.ReadStateAsync();
        Assert.That(disabled, Is.Not.EqualTo(normal), "Disabled has its own appearance");
        var disabledPixels = await button.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled, Path = file + "-disabled.png" });
        var difference = await ScreenshotComparer.CompareAsync(Page, normalPixels, disabledPixels);
        Assert.That(difference?.Changed, Is.GreaterThan(20), "Disabled changes rendered pixels");
        await button.HoverAsync();
        await Page.Mouse.DownAsync();
        Assert.That(await gallery.ReadStateAsync(), Is.EqualTo(disabled), "Disabled cannot acquire pressed styling");
        await Page.Mouse.UpAsync();
        await button.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*3$"));

        await gallery.SetStateAsync(themeOnly ? "IsBusy" : "Loading", true);
        await gallery.SetStateAsync("Disabled", false);
        await Assertions.Expect(button).ToBeDisabledAsync();
        await button.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*3$"));
        await Assertions.Expect(button).ToHaveAccessibleNameAsync(name);
        await gallery.SetStateAsync(themeOnly ? "IsBusy" : "Loading", false);
        if (!themeOnly)
        {
            await gallery.SetStateAsync("Skeleton", true);
            await Assertions.Expect(button).ToBeDisabledAsync();
            await button.EvaluateAsync("e => e.click()");
            await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*3$"));
            await gallery.SetStateAsync("Skeleton", false);
        }
        await Assertions.Expect(button).ToBeEnabledAsync();
        await button.ClickAsync();
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*4$"));
        await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{route.Split('/').Last()}-{themeOnly}.png" });
    }

    protected async Task VerifyLoadingGeometryAndNameAsync(string theme, string route)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonPage(Page);
        await gallery.InitializeAsync(route, false);
        var name = await ButtonPage.ReadActionNameAsync(gallery.Button);
        var initial = await gallery.Button.GetBoundsAsync();
        await gallery.SetStateAsync("Loading", true);
        await Assertions.Expect(gallery.Button).ToHaveAttributeAsync("aria-busy", "true");
        var loading = await gallery.Button.GetBoundsAsync();
        Assert.That(loading.Width, Is.EqualTo(initial.Width).Within(1), "Loading preserves width");
        Assert.That(loading.Height, Is.EqualTo(initial.Height).Within(1), "Loading preserves height");
        await Assertions.Expect(gallery.Button).ToHaveAccessibleNameAsync(name);
        await gallery.SetStateAsync("Loading", false);
        await Assertions.Expect(gallery.Button).ToHaveAccessibleNameAsync(name);
    }
}
