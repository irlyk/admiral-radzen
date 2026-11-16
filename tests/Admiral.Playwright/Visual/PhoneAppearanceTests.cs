using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class PhoneAppearanceTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: Phone country flag and keyboard focus change rendered pixels / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: Phone country flag and keyboard focus change rendered pixels / admiral-dark")]
    public async Task PhoneCountryFlagAndKeyboardFocusChangeRenderedPixelsAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/phone-number-input");
        var phone = Page.Locator(".admiral-phone-input").First;
        var select = phone.GetByRole(AriaRole.Combobox);
        var flag = phone.Locator(".admiral-phone-input__country-value svg");
        await flag.WaitForAsync();
        var originalFlag = await flag.ScreenshotAsync(
            new LocatorScreenshotOptions { Animations = ScreenshotAnimations.Disabled });
        var unfocused = await phone.ScreenshotAsync(
            new LocatorScreenshotOptions { Animations = ScreenshotAnimations.Disabled });

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await select.FocusAsync();

        var focused = await phone.ScreenshotAsync(
            new LocatorScreenshotOptions { Animations = ScreenshotAnimations.Disabled });

        // Assert
        var focusDifference = await ScreenshotComparer.CompareAsync(Page, unfocused, focused);

        Assert.That(
            focusDifference?.Changed > 100,
            Is.True,
            "Keyboard focus changes visible pixels in the phone field");

        // Act
        await select.PressAsync("Enter");

        var popup = Page.Locator(".rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)");

        // Act
        await popup.GetByText("Казахстан", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        var kazakhstan = phone.Locator(
            ".admiral-phone-input__country-value [data-admiral-icon-loaded=\"flags/kazakhstan\"] svg");
        var updatedFlag = await kazakhstan.ScreenshotAsync(
            new LocatorScreenshotOptions { Animations = ScreenshotAnimations.Disabled });

        // Assert
        var flagDifference = await ScreenshotComparer.CompareAsync(Page, originalFlag, updatedFlag);

        Assert.That(
            flagDifference?.Ratio > 0.35,
            Is.True,
            "Choosing Kazakhstan replaces the rendered Russian flag pixels");
        Assert.That(
            (await phone.Locator("input[inputmode=\"tel\"]").InputValueAsync()).StartsWith("+7"),
            Is.True,
            "Countries with the same dial code still produce a different flag");

        // Diagnostics
        if (!string.IsNullOrEmpty(Output))
        {
            Directory.CreateDirectory(Output);
            foreach (var (name, image) in new[]
            {
                ("flag-ru", originalFlag),
                ("flag-kz", updatedFlag),
                ("focus", focused)
            })
            {
                await File.WriteAllBytesAsync(Path.Combine(Output, $"phone-{theme}-{name}.png"), (byte[])image);
            }
        }
    }
}
