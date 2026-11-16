using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class ButtonAppearanceTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: Button loading keeps appearance and blocks native submit / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: Button loading keeps appearance and blocks native submit / admiral-dark")]
    public async Task ButtonLoadingKeepsAppearanceAndBlocksNativeSubmitAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/button");
        var section = Page.Locator("#button-loading-heading").Locator("xpath=..");
        var primary = section.Locator(".admiral-button--loading.admiral-button--primary").First;
        var danger = section.Locator(".admiral-button--loading.admiral-button--danger").First;
        Task<ButtonStateSnapshot> ReadButtonStateAsync(ILocator button) => button.ReadSnapshotAsync<ButtonStateSnapshot>(
            """
            (element) => ({
              background: getComputedStyle(element).backgroundColor,
              disabled: element.disabled,
              ariaDisabled: element.getAttribute('aria-disabled'),
              busy: element.getAttribute('aria-busy'),
            })
            """);
        var primaryState = await ReadButtonStateAsync(primary);
        var dangerState = await ReadButtonStateAsync(danger);

        // Assert
        Assert.That(primaryState.Background, Is.Not.EqualTo(dangerState.Background));

        // Assert
        foreach (var value in new[]
        {
            primaryState,
            dangerState
        })
        {
            // Assert
            Assert.That(value.Disabled, Is.EqualTo(false));
            Assert.That(value.AriaDisabled, Is.EqualTo("true"));
            Assert.That(value.Busy, Is.EqualTo("true"));
        }
    }
}
