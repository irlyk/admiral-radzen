using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class SelectPopupTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: Select-family popups fit a short viewport and retain keyboard selection / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: Select-family popups fit a short viewport and retain keyboard selection / admiral-dark")]
    public async Task SelectFamilyPopupsFitAShortViewportAndRetainKeyboardSelectionAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        var viewport = new
        {
            width = 500,
            height = 350
        };
        await Page.SetViewportSizeAsync(viewport.width, viewport.height);
        async Task CheckPopupAsync(ILocator trigger, ILocator popup, string name)
        {
            await popup.WaitForAsync();
            await Page.WaitForTimeoutAsync(300);
            var anchor = await trigger.GetBoundsAsync();
            var panel = await popup.GetBoundsAsync();

            Assert.That(panel.Width > 0 && panel.Height > 0, Is.True, $"{name}: visible trigger and popup");
            Assert.That(
                panel.X >= -1 && panel.X + panel.Width <= viewport.width + 1 && panel.Y >= -1
                    && panel.Y + panel.Height <= viewport.height + 1,
                Is.True,
                $"{name}: popup fits short viewport {JsonSerializer.Serialize(panel)}");
            Assert.That(
                panel.Y + panel.Height <= anchor.Y + 2 || panel.Y >= anchor.Y + anchor.Height - 2,
                Is.True,
                $"{name}: popup does not overlap trigger");
            Assert.That(
                await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"),
                Is.True,
                $"{name}: no horizontal overflow");

            var list = popup.Locator(".rz-dropdown-items-wrapper");
            if (await list.CountAsync() != 0)
            {
                var metrics = await list.ReadSnapshotAsync<ScrollMetrics>(
                    "e => ({ scrollHeight: e.scrollHeight, clientHeight: e.clientHeight })");
                if (metrics.ScrollHeight > metrics.ClientHeight + 2)
                {
                    await list.EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");

                    Assert.That(
                        await list.EvaluateAsync<bool>("e => e.scrollTop > 0"),
                        Is.True,
                        $"{name}: options remain scrollable");
                }
            }
        }

        try
        {
            await gallery.NavigateAsync("/components/select");
            var select = Page.Locator(".admiral-component-playground .rz-dropdown").First;

            // Act
            await select.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");

            var popup = Page.Locator(".rz-dropdown-panel:visible").Last;

            // Act
            await CheckPopupAsync(select, popup, "AdmiralSelectField");
            await Page.Keyboard.PressAsync("ArrowDown");
            await Page.Keyboard.PressAsync("Enter");

            // Assert
            await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            // Act
            await gallery.NavigateAsync("/components/drop-down");

            var admiralDropDown = Page.Locator("[data-testid=\"drop-down-admiral-demo\"] .rz-dropdown").First;

            // Act
            await admiralDropDown.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");

            var admiralPopup = Page.Locator(".rz-dropdown-panel:visible").Last;

            // Act
            await CheckPopupAsync(admiralDropDown, admiralPopup, "AdmiralDropDown");
            await Page.Keyboard.PressAsync("Escape");
            await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

            var direct = Page.Locator("[data-testid=\"drop-down-radzen-demo\"] .rz-dropdown").First;

            // Act
            await direct.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");

            var directPopup = Page.Locator(".rz-dropdown-panel:visible").Last;

            // Act
            await CheckPopupAsync(direct, directPopup, "direct RadzenDropDown");
            await Page.Keyboard.PressAsync("Escape");
            await gallery.NavigateAsync("/components/phone-number-input");

            var phone = Page.Locator(".admiral-phone-input .rz-dropdown").First;

            // Act
            await phone.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");

            var phonePopup = Page.Locator(".rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)").First;

            // Act
            await CheckPopupAsync(phone, phonePopup, "PhoneNumberInput country Select");
            await Page.Keyboard.PressAsync("Escape");
        }
        finally
        {
            // Cleanup
            await Page.SetViewportSizeAsync(1440, 900);
        }
    }
}
