using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class CarouselKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Carousel: keyboard activates next and pager controls / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Carousel: keyboard activates next and pager controls / admiral-dark")]
    public async Task CarouselKeyboardActivatesNextAndPagerControlsAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/carousel");
        var carousel = Page.Locator(".admiral-carousel").First;
        var before = await carousel.Locator(".rz-carousel-pager-button").EvaluateAllAsync<double>(
            "es => es.findIndex(e => e.classList.contains('rz-state-active'))");

        // Act
        await carousel.Locator(".rz-carousel-next").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "index => [...document.querySelector('.admiral-carousel').querySelectorAll('.rz-carousel-pager-button')].findIndex(e => e.classList.contains('rz-state-active')) !== index",
            before);

        var pager = carousel.Locator(".rz-carousel-pager-button").First;

        // Act
        await pager.FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-carousel .rz-carousel-pager-button').classList.contains('rz-state-active')");
    }
}
