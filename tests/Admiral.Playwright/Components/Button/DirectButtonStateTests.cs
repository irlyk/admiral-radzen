using Admiral.Playwright.Pages.Components.Button;

namespace Admiral.Playwright.Components.Button;

[Category("Button")]
public sealed class DirectButtonStateTests : GalleryTest
{
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task DisabledAndBusyIgnoreShadeAcrossVariantsAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonPage(Page);
        await gallery.InitializeAsync("/components/button", true);
        var expectedColor = theme == "admiral" ? "rgb(178, 181, 184)" : "rgb(81, 83, 87)";
        foreach (var variant in new[] { "Filled", "Flat", "Outlined", "Text" })
        {
            await gallery.SetOptionAsync("Variant", variant);
            await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex("rz-variant-" + variant.ToLowerInvariant()));
            await gallery.SetStateAsync("Disabled", true);
            foreach (var shade in new[] { "Lighter", "Light", "Default", "Dark", "Darker" })
            {
                await gallery.SetOptionAsync("Shade", shade);
                await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex("rz-shade-" + shade.ToLowerInvariant()));
                await Assertions.Expect(gallery.Button).ToHaveCSSAsync("color", expectedColor);
            }
            var disabled = await gallery.ReadStateAsync();
            await gallery.SetStateAsync("IsBusy", true);
            await gallery.SetStateAsync("Disabled", false);
            await Assertions.Expect(gallery.Button).ToBeDisabledAsync();
            Assert.That(await gallery.ReadStateAsync(), Is.EqualTo(disabled), "Busy uses the same blocked palette");
            await gallery.Button.EvaluateAsync("e => e.click()");
            await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*0$"));
            await gallery.SetStateAsync("IsBusy", false);
        }
    }

    [TestCase("admiral", "Filled")]
    [TestCase("admiral", "Flat")]
    [TestCase("admiral", "Outlined")]
    [TestCase("admiral", "Text")]
    [TestCase("admiral-dark", "Filled")]
    [TestCase("admiral-dark", "Flat")]
    [TestCase("admiral-dark", "Outlined")]
    [TestCase("admiral-dark", "Text")]
    public async Task DisabledPaletteWinsForEveryDirectButtonStyleAsync(string theme, string variant)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonPage(Page);
        await gallery.InitializeAsync("/components/button", true);
        await gallery.SetOptionAsync("Variant", variant);
        await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex("rz-variant-" + variant.ToLowerInvariant()));
        var expectedColor = theme == "admiral" ? "rgb(178, 181, 184)" : "rgb(81, 83, 87)";
        var expectedBackground = theme == "admiral" ? "rgb(234, 235, 236)" : "rgb(37, 38, 39)";

        using (Assert.EnterMultipleScope())
        {
            foreach (var style in new[] { "Primary", "Secondary", "Light", "Base", "Dark", "Success", "Danger", "Warning", "Info" })
            {
                await gallery.SetStateAsync("Disabled", false);
                await gallery.SetOptionAsync("ButtonStyle", style);
                await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex(@"\brz-" + style.ToLowerInvariant() + @"\b"));
                await Page.Mouse.MoveAsync(0, 0);
                var enabled = await gallery.ReadStateAsync();
                await gallery.SetStateAsync("Disabled", true);
                await Assertions.Expect(gallery.Button).ToBeDisabledAsync();
                var disabled = await gallery.ReadStateAsync();
                Assert.That(disabled.Color, Is.EqualTo(expectedColor), $"{variant}/{style}: disabled foreground");
                Assert.That(disabled.Background, Is.EqualTo(variant == "Filled" ? expectedBackground : "rgba(0, 0, 0, 0)"),
                    $"{variant}/{style}: disabled surface");
                if (variant == "Outlined")
                {
                    Assert.That(disabled.Shadow, Does.Contain(expectedColor), style + ": disabled outline is neutral");
                }
                await Assertions.Expect(gallery.Button.Locator(".rzi").First).ToHaveCSSAsync("color", expectedColor);
                Assert.That(disabled, Is.Not.EqualTo(enabled), $"{variant}/{style}: disabled must look different");
                await gallery.Button.HoverAsync();
                Assert.That(await gallery.ReadStateAsync(), Is.EqualTo(disabled), $"{variant}/{style}: disabled hover is inert");
                await gallery.Button.EvaluateAsync("e => e.click()");
                await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*0$"));
            }
        }
        await gallery.Preview.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{variant}-disabled.png" });
    }
}
