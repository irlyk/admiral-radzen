using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class CheckboxAppearanceTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: Checkbox demos begin unchecked in Admiral and direct Radzen tabs / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: Checkbox demos begin unchecked in Admiral and direct Radzen tabs / admiral-dark")]
    public async Task CheckboxDemosBeginUncheckedInAdmiralAndDirectRadzenTabsAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/checkbox");

        // Act & assert
        foreach (var (tab, demo, result) in new[]
        {
            ("admiral", "checkbox-admiral-demo", "checkbox-admiral-result"),
            ("theme", "checkbox-radzen-demo", "checkbox-radzen-result")
        })
        {
            // Act
            await Page.Locator($".component-doc-tabs [data-doc-tab=\"{tab}\"]").ClickAsync();

            var checkbox = Page.GetByTestId(demo).GetByRole(AriaRole.Checkbox).First;

            // Assert
            await checkbox.WaitForAsync();

            Assert.That(await checkbox.GetAttributeAsync("aria-checked"), Is.EqualTo("false"));
            Assert.That(
                await Page.GetByTestId(result).InnerTextAsync(),
                Does.Match(new Regex("Changes: 0|Изменений: 0")));

            var box = Page.GetByTestId(demo).Locator(".rz-chkbox-box").First;

            Assert.That(
                await box.EvaluateAsync<string[]>(
                    """
                (element) => {
                  const style = getComputedStyle(element);
                  return [style.transitionProperty, style.transitionDuration, style.transitionTimingFunction];
                }
                """),
                Is.EqualTo(new[] { "background-color", "0.1s", "ease-in" }));

            // Act
            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });

            // Assert
            Assert.That(await box.GetCssAsync("transition-property"), Is.EqualTo("none"));

            // Act
            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        }
    }
}
