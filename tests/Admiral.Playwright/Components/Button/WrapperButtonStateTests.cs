using Admiral.Playwright.Pages.Components.Button;

namespace Admiral.Playwright.Components.Button;

[Category("Button")]
public sealed class WrapperButtonStateTests : GalleryTest
{
    public static IEnumerable<TestCaseData> Families()
    {
        foreach (var theme in new[] { "admiral", "admiral-dark" })
        {
            yield return new TestCaseData(theme, "/components/button",
                new[] { "Primary", "Secondary", "Tertiary", "Ghost", "White", "Danger", "Success" },
                new[] { "S", "M", "L", "Xl" }, new[] { 32, 40, 48, 56 });
            yield return new TestCaseData(theme, "/components/icon-button",
                new[] { "Primary", "Secondary", "Danger" },
                new[] { "S", "M", "L", "Xl" }, new[] { 32, 40, 48, 56 });
            yield return new TestCaseData(theme, "/components/text-button",
                new[] { "Primary", "Secondary" },
                new[] { "S", "M" }, new[] { 20, 24 });
        }
    }

    [TestCaseSource(nameof(Families))]
    public async Task DimensionsAndDisabledAppearancesAsync(string theme, string route, string[] appearances, string[] sizes, int[] heights)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonPage(Page);
        await gallery.InitializeAsync(route, false);
        for (var i = 0; i < sizes.Length; i++)
        {
            await gallery.SetOptionAsync("Dimension", sizes[i]);
            var sizeClass = new[] { "xs", "sm", "md", "lg" }[i];
            await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex("rz-button-" + sizeClass));
            Assert.That((await gallery.Button.GetBoundsAsync()).Height, Is.EqualTo(heights[i]).Within(1), sizes[i]);
        }
        foreach (var appearance in appearances)
        {
            await gallery.SetStateAsync("Disabled", false);
            await gallery.SetOptionAsync("Appearance", appearance);
            await gallery.SetStateAsync("Disabled", true);
            var expectedColor = appearance == "White" || theme == "admiral-dark"
                ? "rgb(81, 83, 87)" : "rgb(178, 181, 184)";
            await Assertions.Expect(gallery.Button).ToHaveCSSAsync("color", expectedColor);
            var disabled = await gallery.ReadStateAsync();
            await gallery.Button.HoverAsync();
            Assert.That(await gallery.ReadStateAsync(), Is.EqualTo(disabled), appearance + ": disabled has no hover");
            await gallery.Button.EvaluateAsync("e => e.click()");
            await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*0$"));
        }
    }

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task DisplayAsDisabledChangesOnlyAppearanceAsync(string theme)
    {
        await StartPageAsync(theme, "en");
        var gallery = new ButtonPage(Page);
        await gallery.InitializeAsync("/components/button", false);
        var before = await gallery.ReadStateAsync();
        await gallery.SetStateAsync("DisplayAsDisabled", true);
        await Assertions.Expect(gallery.Button).ToHaveClassAsync(new Regex("admiral-button--display-disabled"));
        Assert.That(await gallery.ReadStateAsync(), Is.Not.EqualTo(before));
        await Assertions.Expect(gallery.Button).ToBeEnabledAsync();
        await gallery.Button.ClickAsync();
        await Assertions.Expect(gallery.Result).ToHaveTextAsync(new Regex(@":\s*1$"));
    }
}
