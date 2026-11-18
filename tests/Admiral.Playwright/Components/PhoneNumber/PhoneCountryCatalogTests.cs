using Admiral.Playwright.Pages.Components.PhoneNumber;

namespace Admiral.Playwright.Components.PhoneNumber;

[Category("PhoneNumber")]
public sealed class PhoneCountryCatalogTests : GalleryTest
{
    [TestCase("admiral", "ru")]
    [TestCase("admiral", "en")]
    [TestCase("admiral-dark", "ru")]
    [TestCase("admiral-dark", "en")]
    public async Task AdditionalCountriesReplaceDialCodeAndRenderTheirFlagsAsync(string theme, string culture)
    {
        await StartPageAsync(theme, culture);
        var phone = new PhoneNumberPage(Page);
        await phone.InitializeAsync();
        foreach (var (name, dialCode, flag) in new[]
        {
            ("Германия", "+49", "germany"),
            ("Япония", "+81", "japan"),
            ("Канада", "+1", "canada")
        })
        {
            await phone.ChooseCountryAsync(name);
            await Assertions.Expect(phone.Country).ToHaveAttributeAsync("aria-label", name);
            await Assertions.Expect(phone.Input).ToHaveValueAsync($"{dialCode} 999 123-45-67");
            await Assertions.Expect(phone.Phone.Locator($"[data-admiral-icon-loaded='flags/{flag}'] svg"))
                .ToBeVisibleAsync();
        }
        // Shared +1 does not force an explicitly selected Canada back to the USA.
        await phone.Input.FillAsync("+1 202 555-0123");
        await phone.Input.PressAsync("Tab");
        await Assertions.Expect(phone.Country).ToHaveAttributeAsync("aria-label", "Канада");
    }
}
