using Admiral.Playwright.Pages.Components.MenuAction;

namespace Admiral.Playwright.Components.MultiButton;

public abstract class MenuActionTestBase : GalleryTest
{
    protected async Task VerifyKeyboardSelectionDisabledAsync(string theme, string route, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, route, themeOnly);
        await gallery.InitializeAsync();
        var multi = route.EndsWith("/multi-button");
        if (multi)
        {
            await gallery.Main.ClickAsync();
            await Assertions.Expect(gallery.Result).ToContainTextAsync("Main: 1 · Actions: 0");
            await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        }
        await gallery.Toggle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(gallery.Popup).ToBeVisibleAsync();
        var blocked = gallery.Popup.GetByRole(AriaRole.Menuitem, new() { Name = "Unavailable", Exact = true });
        await blocked.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Result).ToContainTextAsync("Actions: 0");
        if (themeOnly)
        {
            await Page.Keyboard.PressAsync("ArrowDown");
        }
        else
        {
            await Assertions.Expect(gallery.Popup.GetByRole(AriaRole.Menuitem, new() { Name = "Print", Exact = true })).ToBeFocusedAsync();
        }

        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        await Assertions.Expect(gallery.Result).ToContainTextAsync("Actions: 1 · Last: print");
        await Assertions.Expect(gallery.Toggle).ToBeFocusedAsync();
        await gallery.Toggle.ClickAsync();
        await Assertions.Expect(gallery.Popup).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        await Assertions.Expect(gallery.Toggle).ToBeFocusedAsync();
        await gallery.Toggle.ClickAsync();
        await Assertions.Expect(gallery.Popup).ToBeVisibleAsync();
        await gallery.CloseOutsideAsync();
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Tab");
        await gallery.Toggle.FocusAsync();
        await Assertions.Expect(gallery.Toggle).ToHaveCSSAsync("outline-width", "2px");
        await Page.Mouse.MoveAsync(0, 0);
        var normal = await gallery.Preview.ScreenshotAsync();
        await gallery.SetAsync("Disabled", true);
        await Assertions.Expect(gallery.Toggle).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Main).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Toggle).ToHaveCSSAsync("color", theme == "admiral" ? "rgb(178, 181, 184)" : "rgb(81, 83, 87)");
        await gallery.Toggle.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        var disabled = await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{themeOnly}-disabled.png" });
        Assert.That((await ScreenshotComparer.CompareAsync(Page, normal, disabled))?.Changed, Is.GreaterThan(20));
    }

    protected async Task VerifyDisableFromOpenMenuAsync(string theme, string route)
    {
        await StartPageAsync(theme, "en");
        var gallery = new MenuActionPage(Page, route);
        await gallery.InitializeAsync();
        await gallery.Toggle.ClickAsync();
        await gallery.Popup.GetByRole(AriaRole.Menuitem, new() { Name = "Disable trigger", Exact = true }).ClickAsync();
        await Assertions.Expect(gallery.Toggle).ToBeDisabledAsync();
        await Assertions.Expect(gallery.Popup).ToHaveCountAsync(0);
        await Assertions.Expect(gallery.Toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await gallery.SetAsync("Disabled", false);
        await gallery.Toggle.ClickAsync();
        await Assertions.Expect(gallery.Popup).ToBeVisibleAsync();
    }
    protected async Task VerifySmallViewportAsync(string theme, string route)
    {
        await StartPageAsync(theme, "en", 390, 480);
        var gallery = new MenuActionPage(Page, route);
        await gallery.InitializeAsync();
        await gallery.Toggle.ClickAsync();
        await Assertions.Expect(gallery.Popup).ToBeVisibleAsync();
        var bounds = await gallery.Popup.GetBoundsAsync();
        Assert.That(bounds.X, Is.GreaterThanOrEqualTo(16));
        Assert.That(bounds.X + bounds.Width, Is.LessThanOrEqualTo(374));
        Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(16));
        Assert.That(bounds.Y + bounds.Height, Is.LessThanOrEqualTo(464));
        await Page.ScreenshotAsync(new() { Path = $"{Output}/{theme}-small-viewport.png" });
    }
}
