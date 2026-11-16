using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class SliderKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Slider: ArrowRight adjusts value and exposes live slider semantics / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Slider: ArrowRight adjusts value and exposes live slider semantics / admiral-dark")]
    public async Task SliderArrowRightAdjustsValueAndExposesLiveSliderSemanticsAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/slider");
        var handle = Page.Locator(".admiral-slider [role=\"slider\"]").First;
        var before = NumericValue.ParseInvariant(await handle.GetAttributeAsync("aria-valuenow"));

        // Act
        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await Page.WaitForFunctionAsync(
            "value => Number(document.querySelector('.admiral-slider [role=\"slider\"]').getAttribute('aria-valuenow')) > value",
            before);

        Assert.That(await handle.GetAttributeAsync("aria-label"), Is.Not.Null.And.Not.Empty);
    }
}
