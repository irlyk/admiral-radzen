using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class GridKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Grid: keyboard row focus has color, horizontal cell focus has outline / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Grid: keyboard row focus has color, horizontal cell focus has outline / admiral-dark")]
    public async Task GridKeyboardRowFocusHasColorHorizontalCellFocusHasOutlineAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/tables");
        var playground = Page.GetByTestId("table-playground-scroll");
        var grid = playground.GetByRole(AriaRole.Grid);

        // Act
        await grid.PressAsync("ArrowDown");

        var row = grid.Locator("tr.rz-data-row.rz-state-focused").First;

        // Assert
        await row.WaitForAsync();
        await Page.WaitForFunctionAsync(
            """
            () => {
              const cell = document.querySelector('.rz-data-grid-data tr.rz-data-row.rz-state-focused > td');
              return cell && getComputedStyle(cell).backgroundColor !== 'rgba(0, 0, 0, 0)';
            }
            """);
        var cellColor = await row.Locator("td").First.GetCssAsync("background-color");

        Assert.That(cellColor, Is.Not.EqualTo("rgba(0, 0, 0, 0)"));

        // Act
        await Page.Keyboard.PressAsync("ArrowRight");

        var cell = grid.Locator("td.rz-state-focused").First;

        // Assert
        await cell.WaitForAsync();

        Assert.That(await cell.GetCssAsync("outline-style"), Is.Not.EqualTo("none"));
    }
}
