using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class PaginationAppearanceTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: Pagination dropdown geometry in default, hover, focus and open states / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: Pagination dropdown geometry in default, hover, focus and open states / admiral-dark")]
    public async Task PaginationDropdownGeometryInDefaultHoverFocusAndOpenStatesAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/tables");
        var table = Page.Locator("section[aria-labelledby=\"table-l-heading\"]");
        var pagination = table.Locator(".admiral-pagination-one:not(.admiral-pagination-one--simple)");
        await pagination.WaitForAsync();
        var selects = pagination.Locator(".admiral-pagination-one__select.rz-dropdown");

        // Assert
        await Assertions.Expect(selects).ToHaveCountAsync(2);

        // Act & assert
        for (var i = 0; i < 2; i++)
        {
            var select = selects.Nth(i);

            // Assert
            var @base = await select.GetBoundsAsync();
            var label = await select.Locator(".rz-dropdown-label").GetBoundsAsync();

            Assert.That(
                label.X >= @base.X - 1 && label.X + label.Width <= @base.X + @base.Width + 1,
                Is.True,
                $"Dropdown label is contained by outer control {i}");

            async Task<DropdownGeometry> GetGeometryAsync()
            {
                var box = await select.GetBoundsAsync();
                var border = await select.GetCssAsync("border-top-width");
                return new DropdownGeometry(box, CssPixels.Parse(border));
            }

            // Assert
            var start = await GetGeometryAsync();

            // Act
            await select.HoverAsync();

            // Assert
            var hover = await GetGeometryAsync();

            // Act
            await Page.Keyboard.PressAsync("Tab");
            await select.FocusAsync();
            await Page.WaitForTimeoutAsync(250);

            // Assert
            var focus = await GetGeometryAsync();
            var focusStyle = await select.ReadSnapshotAsync<DropdownFocusStyle>("""
                (e) => {
                  const style = getComputedStyle(e);
                  const probe = document.createElement('span');
                  probe.style.color = style.getPropertyValue('--rz-primary').trim();
                  e.appendChild(probe);
                  const primary = getComputedStyle(probe).color;
                  probe.remove();
                  return {
                    visible: e.matches(':focus-visible'),
                    color: style.borderTopColor,
                    width: style.borderTopWidth,
                    primary,
                  };
                }
                """);

            Assert.That(focusStyle.Visible, Is.True, $"Dropdown {i} has keyboard focus-visible");
            Assert.That(focusStyle.Width, Is.EqualTo("2px"), $"Dropdown {i} focus border is 2px");
            Assert.That(
                focusStyle.Color,
                Is.EqualTo(focusStyle.Primary),
                $"Dropdown {i} focus border uses the primary color");

            // Act
            await Page.Locator("h1").ClickAsync();
            await select.ClickAsync();

            // Assert
            await Page.Locator(".rz-dropdown-panel:visible").First.WaitForAsync();
            await Page.WaitForTimeoutAsync(250);
            var open = await GetGeometryAsync();
            var mouseStyle = await select.ReadSnapshotAsync<DropdownMouseStyle>("""
                (e) => ({
                  visible: e.matches(':focus-visible'),
                  color: getComputedStyle(e).borderTopColor,
                  width: getComputedStyle(e).borderTopWidth,
                })
                """);

            Assert.That(mouseStyle.Visible, Is.EqualTo(false), $"Dropdown {i} mouse focus has no keyboard ring");
            Assert.That(
                mouseStyle.Color,
                Is.EqualTo("rgba(0, 0, 0, 0)"),
                $"Dropdown {i} mouse focus border remains transparent");
            Assert.That(mouseStyle.Width, Is.EqualTo("2px"), $"Dropdown {i} mouse focus preserves border geometry");

            // Assert
            foreach (var (state, value) in new[]
            {
                ("hover", hover),
                ("focus", focus),
                ("open", open)
            })
            {
                // Assert
                Assert.That(
                    Math.Abs(value.Box.Width - start.Box.Width) <= 1
                        && Math.Abs(value.Box.Height - start.Box.Height) <= 1,
                    Is.True,
                    $"Dropdown {i} geometry remains stable in {state}");
                Assert.That(
                    Math.Abs(value.Border - start.Border) <= 1,
                    Is.True,
                    $"Dropdown {i} border remains stable in {state}");
            }

            // Act
            await Page.Keyboard.PressAsync("Escape");
        }

        // Act
        await gallery.NavigateAsync("/regression/inputs");

        var direct = Page.Locator("#input-currency.rz-dropdown").First;

        // Assert
        await direct.WaitForAsync();

        Assert.That(
            await direct.GetCssAsync("border-top-width"),
            Is.EqualTo("1px"),
            "Direct Radzen dropdown retains its 1px input border");
    }
}
