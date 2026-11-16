using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class ImageViewerKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: ImageViewer: modal focus trap and keyboard close / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: ImageViewer: modal focus trap and keyboard close / admiral-dark")]
    public async Task ImageViewerModalFocusTrapAndKeyboardCloseAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/image-viewer");
        var opener = Page.Locator(".admiral-image-viewer__miniature").First;

        // Act
        await opener.ClickAsync();

        // Assert
        await Page.WaitForSelectorAsync(".admiral-image-viewer__overlay");
        await Page.WaitForFunctionAsync(
            "() => document.activeElement?.classList.contains('admiral-image-viewer__close')");

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(
            await Page.EvaluateAsync<bool>(
                "() => !!document.activeElement.closest('.admiral-image-viewer__overlay')"),
            Is.True);

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(
            await Page.EvaluateAsync<bool>(
                "() => document.activeElement.classList.contains('admiral-image-viewer__close')"),
            Is.True);

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.WaitForSelectorAsync(
            ".admiral-image-viewer__overlay",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });

        Assert.That(await opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
    }
}
