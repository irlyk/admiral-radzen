using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class TextInputAppearanceTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: TextInput status borders use Admiral error and success shades / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: TextInput status borders use Admiral error and success shades / admiral-dark")]
    public async Task TextInputStatusBordersUseAdmiralErrorAndSuccessShadesAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/text-input");
        var expected = (theme == "admiral-dark")
            ? new
            {
                error = new[]
            {
                "rgb(234, 60, 60)",
                "rgb(245, 95, 95)"
            },
                success = new[]
            {
                "rgb(22, 145, 63)",
                "rgb(31, 169, 81)"
            }
            }
            : new
            {
                error = new[]
            {
                "rgb(217, 32, 32)",
                "rgb(186, 23, 23)"
            },
                success = new[]
            {
                "rgb(27, 160, 73)",
                "rgb(18, 130, 56)"
            }
            };

        // Act & assert
        foreach (var (status, colors) in new[]
        {
            ("error", expected.error),
            ("success", expected.success)
        })
        {
            var normal = colors[0];
            var hover = colors[1];
            var input = Page.Locator($".admiral-text-input--{status} .admiral-text-input__control").First;
            Task<string> ReadBorderColorAsync() => input.GetCssAsync("border-top-color");

            // Assert
            Assert.That(await ReadBorderColorAsync(), Is.EqualTo(normal));

            // Act
            await input.HoverAsync();

            // Assert
            Assert.That(await ReadBorderColorAsync(), Is.EqualTo(hover));

            // Act
            await input.FocusAsync();

            // Assert
            Assert.That(await ReadBorderColorAsync(), Is.EqualTo(normal));
        }
    }
}
