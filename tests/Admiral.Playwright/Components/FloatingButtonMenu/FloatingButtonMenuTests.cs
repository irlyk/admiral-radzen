using Admiral.Playwright.Pages.Components.FloatingButtonMenu;

namespace Admiral.Playwright.Components.FloatingButtonMenu;

[Category("Button")]
public sealed class FloatingButtonMenuTests : GalleryTest
{
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task TriggerIconChangesOnOpenAndEscapeRestoresFocusAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new FloatingButtonMenuPage(Page);
        await gallery.InitializeAsync();
        var menu = gallery.Menu;
        var trigger = gallery.Trigger;
        await trigger.Locator("svg").WaitForAsync();
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(trigger.Locator("[data-admiral-icon-loaded='service/close-outline'] svg")).ToBeVisibleAsync();
        await Assertions.Expect(menu.Locator(".admiral-floating-button-menu__items")).ToHaveCSSAsync("opacity", "1");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Assertions.Expect(menu.Locator(".admiral-floating-button-menu__items button").Last).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
        await Assertions.Expect(trigger.Locator("[data-admiral-icon-loaded='system/email-outline'] svg")).ToBeVisibleAsync();
    }

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task UnboundIsOpenAppliesExternalChangesWithoutResettingLocalToggleAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new FloatingButtonMenuPage(Page);
        await gallery.InitializeAsync();
        var trigger = gallery.UnboundTrigger;
        await gallery.SetAsync("IsOpen · unbound", true);
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await gallery.SelectSizeAsync("Xl");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await gallery.SetAsync("IsOpen · unbound", false);
        await gallery.SetAsync("IsOpen · unbound", true);
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task MixedDisabledActionsAndOutsideCloseAsync(string theme, bool themeOnly)
    {
        await StartPageAsync(theme, "en");
        var gallery = new FloatingButtonMenuPage(Page);
        await gallery.InitializeAsync(themeOnly);
        var trigger = gallery.Trigger;
        await gallery.SetAsync("Item Disabled", true);
        await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(trigger.Locator("[data-admiral-icon-loaded='service/close-outline'] svg")).ToBeVisibleAsync();
        var actions = gallery.Actions;
        await Assertions.Expect(actions.First).ToBeDisabledAsync();
        var disabledSurface = themeOnly ? actions.First : actions.First.Locator(".admiral-floating-button__content");
        await Assertions.Expect(disabledSurface).ToHaveCSSAsync("color", theme == "admiral" ? "rgb(178, 181, 184)" : "rgb(81, 83, 87)");
        await actions.First.EvaluateAsync("e => e.click()");
        await actions.Last.ClickAsync();
        await Assertions.Expect(gallery.Result).ToContainTextAsync("Печать");
        if (!themeOnly)
        {
            await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
            await gallery.CloseOutsideAsync();
        }
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(trigger.Locator("[data-admiral-icon-loaded='system/email-outline'] svg")).ToBeVisibleAsync();
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await gallery.CloseOutsideAsync(themeOnly);
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await gallery.SetAsync("Disabled", true);
        await Assertions.Expect(trigger).ToBeDisabledAsync();
        var disabledTrigger = themeOnly ? trigger : trigger.Locator(".admiral-floating-button__content");
        await Assertions.Expect(disabledTrigger).ToHaveCSSAsync("color", theme == "admiral" ? "rgb(178, 181, 184)" : "rgb(81, 83, 87)");
        await trigger.EvaluateAsync("e => e.click()");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task ActionSizeGapAndFixedPositionFollowMenuAsync(string theme)
    {
        await StartPageAsync(theme, "en", 390, 800);
        var gallery = new FloatingButtonMenuPage(Page);
        await gallery.InitializeAsync();
        foreach (var size in new[] { "Medium", "Xl" })
        {
            await gallery.SelectSizeAsync(size);
            await Assertions.Expect(gallery.Trigger).ToHaveClassAsync(new Regex(size == "Xl" ? "--xl" : "--m"));
            await gallery.Trigger.ClickAsync();
            await Assertions.Expect(gallery.Items).ToHaveCSSAsync("opacity", "1");
            var expected = size == "Xl" ? 56 : 40;
            var first = await gallery.Actions.First.GetBoundsAsync();
            var last = await gallery.Actions.Last.GetBoundsAsync();
            Assert.That(first.Width, Is.EqualTo(expected));
            Assert.That(last.Height, Is.EqualTo(expected));
            Assert.That(last.Y - first.Y - first.Height, Is.EqualTo(size == "Xl" ? 16 : 12).Within(1));
            await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{size}-open.png" });
            await gallery.Trigger.ClickAsync();
            await Assertions.Expect(gallery.Trigger).ToHaveAttributeAsync("aria-expanded", "false");
        }
        await gallery.SetAsync("Fixed", true);
        await gallery.SetAsync("Mobile offset", true);
        await Assertions.Expect(gallery.Menu).ToHaveCSSAsync("right", "16px");
        var before = await gallery.Trigger.GetBoundsAsync();
        Assert.That(800 - before.Y - before.Height, Is.EqualTo(16).Within(1));
        await Page.Locator(".admiral-layout__body").EvaluateAsync("e => e.scrollTop = 0");
        var after = await gallery.Trigger.GetBoundsAsync();
        Assert.That(after.Y, Is.EqualTo(before.Y).Within(1));
        await gallery.SetAsync("Disabled", true);
        await Assertions.Expect(gallery.Trigger).ToBeDisabledAsync();
        Assert.That(await gallery.Items.EvaluateAsync<bool>("e => e.inert"), Is.True);
    }
}
