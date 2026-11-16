using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class TooltipKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Tooltip: focused child is described and existing description survives Escape / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Tooltip: focused child is described and existing description survives Escape / admiral-dark")]
    public async Task TooltipFocusedChildIsDescribedAndExistingDescriptionSurvivesEscapeAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/tooltip");
        var trigger = Page.Locator(".admiral-component-playground__preview button").First;
        await trigger.EvaluateAsync("""
            (element) => {
              const description = document.createElement('span');
              description.id = 'existing-tooltip-help';
              description.textContent = 'Existing description';
              element.after(description);
              element.setAttribute('aria-describedby', description.id);
            }
            """);

        // Act
        await trigger.FocusAsync();

        var tooltip = Page.GetByRole(AriaRole.Tooltip);

        // Assert
        await tooltip.WaitForAsync();
        var id = await tooltip.GetAttributeAsync("id");
        await Page.WaitForFunctionAsync("""
            (id) =>
              document
                .querySelector('.admiral-component-playground__preview button')
                ?.getAttribute('aria-describedby')
                ?.split(/\s+/)
                .includes(id)
            """, id);

        var session = await Context.NewCDPSessionAsync(Page);
        var nodes = (await session.SendCommandAsync<AccessibilityTreeResponse>("Accessibility.getFullAXTree")).Nodes;

        // Assert
        var button = nodes.FirstOrDefault(
            (node) => node.Role?.Value == "button" && node.Name?.Value == "Hover or focus");

        Assert.That(button?.Description?.Value ?? "", Does.Match(new Regex("Existing description")));
        Assert.That(button?.Description?.Value ?? "", Does.Match(new Regex("Helpful details")));

        // Act
        await session.DetachAsync();
        await trigger.PressAsync("Escape");

        // Assert
        await tooltip.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await Page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('.admiral-component-playground__preview button')
                ?.getAttribute('aria-describedby') === 'existing-tooltip-help'
            """);
    }
}
