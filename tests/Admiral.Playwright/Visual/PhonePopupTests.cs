using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class PhonePopupTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: Phone country popup stays within viewport and responds to keyboard / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: Phone country popup stays within viewport and responds to keyboard / admiral-dark")]
    public async Task PhoneCountryPopupStaysWithinViewportAndRespondsToKeyboardAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);

        // Act & assert
        foreach (var (width, height) in new[]
        {
            (240, 800),
            (320, 800),
            (390, 800),
            (320, 320)
        })
        {
            // Act
            await Page.SetViewportSizeAsync(width, height);
            await gallery.NavigateAsync("/components/phone-number-input");

            var select = Page.Locator(".admiral-phone-input .rz-dropdown").First;

            // Act
            await select.ClickAsync();

            var popup = Page.Locator(".rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)").First;

            // Assert
            await popup.WaitForAsync();
            await Page.WaitForTimeoutAsync(300);
            var box = await popup.GetBoundsAsync();

            Assert.That(
                box.X >= -1 && box.X + box.Width <= width + 1 && box.Y >= -1
                    && box.Y + box.Height <= height + 1,
                Is.True,
                $"Phone country popup fits {width}×{height}px viewport");

            var list = await popup.Locator(".rz-dropdown-items-wrapper").GetBoundsAsync();
            var dialCode = await popup.Locator(".admiral-phone-input__dial-code").First.GetBoundsAsync();

            Assert.That(
                list.X >= box.X - 1 && list.X + list.Width <= box.X + box.Width + 1 && dialCode.X >= list.X - 1
                    && dialCode.X + dialCode.Width <= list.X + list.Width + 1,
                Is.True,
                $"Phone country list and dial code fit {width}×{height}px popup");

            var search = popup.Locator("input").First;

            // Act
            await search.FillAsync("United");

            // Assert
            await Page.WaitForFunctionAsync(
                "() => document.querySelectorAll('.rz-dropdown-panel.rz-open .rz-dropdown-item').length === 2");
            await Page.WaitForTimeoutAsync(350);

            // Act
            await search.PressAsync("ArrowDown");

            // Assert
            await popup.Locator(".rz-dropdown-item.rz-state-highlight").First.WaitForAsync();

            Assert.That(await search.InputValueAsync(), Is.EqualTo("United"));

            // Act
            await search.PressAsync("Enter");

            // Assert
            await Page.WaitForFunctionAsync(
                "() => /United/.test(document.querySelector('.admiral-phone-input .rz-dropdown')?.getAttribute('aria-label') ?? '')");
            await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            // Act
            await select.ClickAsync();

            // Assert
            await Page.Locator(".rz-dropdown-panel:visible").First.WaitForAsync();

            // Act
            await Page.Keyboard.PressAsync("Escape");

            // Assert
            await Page.Locator(".rz-dropdown-panel:visible").First.WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        }
    }
}
