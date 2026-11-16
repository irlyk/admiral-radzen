using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Visual;

[Category("Visual")]
public sealed class GroupActionsAppearanceTests : GalleryTest
{
    [TestCase("admiral", TestName = "Visual: GroupActions text action focus and alignment across sizes / admiral")]
    [TestCase("admiral-dark", TestName = "Visual: GroupActions text action focus and alignment across sizes / admiral-dark")]
    public async Task GroupActionsTextActionFocusAndAlignmentAcrossSizesAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/action-bar");
        var preview = Page.Locator(".action-bar-demo-preview");
        var pane = preview.Locator(".admiral-group-actions-pane");
        var action = pane.Locator(".admiral-group-actions-pane__actions > button").First;
        var size = Page.Locator(".admiral-component-controls select").First;

        // Act & assert
        foreach (var option in new[]
        {
            "Small",
            "Medium",
            "Large",
            "ExtraLarge"
        })
        {
            // Act
            await size.SelectOptionAsync(option);

            // Assert
            await action.WaitForAsync();

            // Act
            await Page.Keyboard.PressAsync("Tab");
            await action.FocusAsync();

            // Assert
            var styles = await action.ReadSnapshotAsync<FocusOutlineStyle>("""
                (element) => {
                  const style = getComputedStyle(element);
                  const clip = getComputedStyle(element.parentElement);
                  return {
                    outline: style.outlineStyle,
                    outlineWidth: parseFloat(style.outlineWidth),
                    outlineOffset: parseFloat(style.outlineOffset),
                    clipMargin: parseFloat(clip.overflowClipMargin) || 0,
                  };
                }
                """);

            Assert.That(styles.Outline, Is.Not.EqualTo("none"), $"Focus outline is present at {option}");
            Assert.That(styles.OutlineWidth > 0, Is.True, $"Focus outline has width at {option}");
            Assert.That(
                styles.ClipMargin >= styles.OutlineWidth + Math.Max(styles.OutlineOffset, 0),
                Is.True,
                $"Focus outline fits inside clip margin at {option}");

            var parent = await pane.GetBoundsAsync();
            var button = await action.GetBoundsAsync();

            Assert.That(
                Math.Abs(button.Y + button.Height / 2 - (parent.Y + parent.Height / 2)),
                Is.LessThanOrEqualTo(2),
                $"Action is vertically aligned at {option}");
            Assert.That(button.Height <= parent.Height + 1, Is.True, $"Action fits pane height at {option}");
        }
    }
}
