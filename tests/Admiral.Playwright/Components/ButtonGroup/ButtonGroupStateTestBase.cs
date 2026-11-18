using Admiral.Playwright.Pages.Components.Button;
using Admiral.Playwright.Pages.Components.ButtonGroup;

namespace Admiral.Playwright.Components.ButtonGroup;

public abstract class ButtonGroupStateTestBase : GalleryTest
{
    protected async Task VerifyGeometryAsync(string theme, bool themeOnly, bool iconOnly, string route)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonGroupPage(Page);
        await gallery.InitializeAsync(route, themeOnly);
        foreach (var appearance in iconOnly ? new[] { "Primary", "Secondary", "Danger" } : new[] { "Primary", "Secondary", "Tertiary" })
        {
            await gallery.SetOptionAsync("Appearance", appearance);
            await Assertions.Expect(gallery.Buttons.First).ToHaveClassAsync(new Regex(
                (iconOnly ? "admiral-icon-button--" : "admiral-button--") + appearance.ToLowerInvariant()));
            var sizes = themeOnly ? new[] { "ExtraSmall", "Small", "Medium", "Large" } : new[] { "S", "M", "L", "Xl" };
            for (var i = 0; i < sizes.Length; i++)
            {
                await gallery.SetOptionAsync(themeOnly ? "Size" : "Dimension", sizes[i]);
                await Assertions.Expect(gallery.Buttons.First).ToHaveClassAsync(new Regex("rz-button-" + new[] { "xs", "sm", "md", "lg" }[i]));
                var geometry = await gallery.MeasureAsync();
                Assert.That(geometry.Buttons.All(b => Math.Abs(b.Bounds.Height - new[] { 32, 40, 48, 56 }[i]) < 1), Is.True);
                if (iconOnly)
                {
                    Assert.That(geometry.Buttons.All(b => Math.Abs(b.Bounds.Width - b.Bounds.Height) < 1), Is.True);
                }
                var expectedGap = iconOnly || appearance == "Secondary" ? -1 : 1;
                Assert.That(geometry.Buttons[1].Bounds.Left - geometry.Buttons[0].Bounds.Right, Is.EqualTo(expectedGap).Within(1));
                Assert.That(geometry.Buttons[2].Bounds.Left - geometry.Buttons[1].Bounds.Right, Is.EqualTo(expectedGap).Within(1));
                Assert.That(geometry.Buttons[0].StartStart, Is.GreaterThan(0));
                Assert.That(geometry.Buttons[0].StartEnd, Is.Zero);
                Assert.That(geometry.Buttons[1].StartStart + geometry.Buttons[1].StartEnd + geometry.Buttons[1].EndStart + geometry.Buttons[1].EndEnd, Is.Zero);
                Assert.That(geometry.Buttons[2].StartStart, Is.Zero);
                Assert.That(geometry.Buttons[2].StartEnd, Is.GreaterThan(0));
            }
        }
        // Logical edges and the shared border also work when direction changes at runtime.
        await gallery.Group.EvaluateAsync("e => e.dir = 'rtl'");
        var rtl = await gallery.MeasureAsync();
        var rtlGap = iconOnly || await gallery.Controls.GetByLabel(new Regex("^Appearance")).InputValueAsync() == "Secondary" ? -1 : 1;
        Assert.That(rtl.Buttons[0].Bounds.Left - rtl.Buttons[1].Bounds.Right, Is.EqualTo(rtlGap).Within(1));
        Assert.That(rtl.Buttons[0].StartStart, Is.GreaterThan(0));
        Assert.That(rtl.Buttons[2].StartEnd, Is.GreaterThan(0));
        await gallery.Group.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{themeOnly}-rtl.png" });
    }

    protected async Task VerifyStatesAsync(string theme, bool themeOnly, bool iconOnly, string route)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonGroupPage(Page);
        await gallery.InitializeAsync(route, themeOnly);
        await gallery.SetStateAsync("Disabled · last", false);
        var first = gallery.Buttons.Nth(0);
        var middle = gallery.Buttons.Nth(1);
        var last = gallery.Buttons.Nth(2);
        await Page.Mouse.MoveAsync(0, 0);
        var normal = await gallery.ReadStateAsync(0);
        await first.HoverAsync();
        var hover = await gallery.ReadStateAsync(0);
        Assert.That(hover, Is.Not.EqualTo(normal), "Hover is visible inside the joined group");
        Assert.That(hover.Image, Is.EqualTo("none"), "Admiral palette must not receive a second Radzen gradient overlay");
        var expectedHover = iconOnly
            ? theme == "admiral" ? "rgba(0, 0, 0, 0.05)" : "rgba(255, 255, 255, 0.06)"
            : theme == "admiral" ? "rgb(0, 70, 226)" : "rgb(82, 144, 255)";
        Assert.That(hover.Background, Is.EqualTo(expectedHover));
        await Page.Mouse.DownAsync();
        var pressed = await gallery.ReadStateAsync(0);
        Assert.That(pressed, Is.Not.EqualTo(hover), "Pressed is distinct from hover");
        Assert.That(pressed.Image, Is.EqualTo("none"));
        var expectedPressed = iconOnly
            ? theme == "admiral" ? "rgba(0, 0, 0, 0.12)" : "rgba(255, 255, 255, 0.12)"
            : theme == "admiral" ? "rgb(1, 50, 176)" : "rgb(104, 159, 255)";
        Assert.That(pressed.Background, Is.EqualTo(expectedPressed));
        await Page.Mouse.UpAsync();
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*1$"));
        await first.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*3$"));
        var middleName = await ButtonPage.ReadActionNameAsync(middle);
        await middle.ClickAsync();
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*4$"));
        await Assertions.Expect(gallery.Result).ToContainTextAsync(middleName);

        await gallery.SetStateAsync("Disabled · last", true);
        await Assertions.Expect(last).ToBeDisabledAsync();
        var expectedDisabled = theme == "admiral" ? "rgb(178, 181, 184)" : "rgb(81, 83, 87)";
        await Assertions.Expect(last).ToHaveCSSAsync("color", expectedDisabled);
        await last.EvaluateAsync("e => e.click()");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*4$"));
        await Page.Keyboard.PressAsync("Tab");
        await first.FocusAsync();
        await Assertions.Expect(first).ToHaveCSSAsync("outline-style", "solid");
        await Assertions.Expect(first).ToHaveCSSAsync("z-index", "1");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(middle).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await gallery.Group.EvaluateAsync<bool>("e => e.contains(document.activeElement)"), Is.False, "Disabled last action is skipped");

        var geometry = await gallery.MeasureAsync();
        var loadingName = await ButtonPage.ReadActionNameAsync(middle);
        await gallery.SetStateAsync(themeOnly ? "IsBusy" : "Loading", true);
        await Assertions.Expect(middle).ToBeDisabledAsync();
        await Assertions.Expect(middle).ToHaveAccessibleNameAsync(loadingName);
        await middle.EvaluateAsync("e => e.click()");
        await first.ClickAsync();
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*5$"));
        if (!themeOnly)
        {
            var loadingGeometry = await gallery.MeasureAsync();
            Assert.That(loadingGeometry.Buttons[1].Bounds.Width, Is.EqualTo(geometry.Buttons[1].Bounds.Width).Within(1));
            await gallery.SetStateAsync("Skeleton", true);
            await middle.EvaluateAsync("e => e.click()");
            await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*5$"));
            await gallery.SetStateAsync("Skeleton", false);
        }
        await gallery.SetStateAsync(themeOnly ? "IsBusy" : "Loading", false);
        await Assertions.Expect(middle).ToBeEnabledAsync();
        await middle.ClickAsync();
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*6$"));
        await gallery.Group.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{themeOnly}-mixed.png" });
    }

    protected async Task VerifyNarrowLayoutAsync(string theme, bool themeOnly, string route)
    {
        await StartPageAsync(theme, "en", 320, 800);
        var gallery = new ButtonGroupPage(Page);
        await gallery.InitializeAsync(route, themeOnly);
        await gallery.SetOptionAsync(themeOnly ? "Size" : "Dimension", themeOnly ? "Large" : "Xl");
        await Assertions.Expect(gallery.Buttons.First).ToHaveClassAsync(new Regex("rz-button-lg"));
        var geometry = await gallery.MeasureAsync();
        Assert.That(geometry.Buttons.All(b => b.Content.Left >= b.Bounds.Left - 1 && b.Content.Right <= b.Bounds.Right + 1), Is.True, "Labels fit their buttons");
        await gallery.SetStateAsync("Disabled · last", false);
        await Assertions.Expect(gallery.Buttons.Last).ToBeEnabledAsync();
        await gallery.Buttons.First.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(gallery.Buttons.Last).ToBeFocusedAsync();
        var last = await gallery.Buttons.Last.GetBoundsAsync();
        var viewport = await gallery.Preview.Locator(".admiral-button-group-viewport").GetBoundsAsync();
        var outset = CssPixels.Parse(await gallery.Buttons.Last.GetCssAsync("outline-width"))
            + Math.Max(0, CssPixels.Parse(await gallery.Buttons.Last.GetCssAsync("outline-offset")));
        Assert.That(last.X - outset, Is.GreaterThanOrEqualTo(viewport.X - 1), "Focus outline fits the scroll area");
        Assert.That(last.X + last.Width + outset, Is.LessThanOrEqualTo(viewport.X + viewport.Width + 1));
        Assert.That(last.X, Is.GreaterThanOrEqualTo(0));
        Assert.That(last.X + last.Width, Is.LessThanOrEqualTo(321), "Keyboard focus scrolls the last action into view");
        Assert.That(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"), Is.True, "No horizontal page overflow");
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@"Clicks:\s*1$"));
        await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{themeOnly}-320.png" });
    }
}
