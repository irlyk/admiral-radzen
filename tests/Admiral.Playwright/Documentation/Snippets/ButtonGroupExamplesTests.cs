using Admiral.Playwright.Pages.Components.ButtonGroup;

namespace Admiral.Playwright.Documentation.Snippets;

[Category("Snippets")]
public sealed class ButtonGroupExamplesTests : GalleryTest
{
    [Test]
    public async Task DirectGroupExamplesCompileInDefaultAndMixedStatesAsync()
    {
        await StartPageAsync("admiral", "en");
        var compiler = await SnippetCompiler.CreateAsync();
        var gallery = new ButtonGroupPage(Page);
        foreach (var route in new[] { "/components/button-groups", "/components/icon-button-group" })
        {
            await gallery.InitializeAsync(route, true);
            foreach (var mixed in new[] { false, true })
            {
                if (mixed)
                {
                    await gallery.SetOptionAsync("Size", "Large");
                    await gallery.SetOptionAsync("Appearance", "Secondary");
                    await gallery.SetStateAsync("Disabled · last", true);
                    await gallery.SetStateAsync("IsBusy", true);
                    await Assertions.Expect(gallery.Buttons.Nth(1)).ToBeDisabledAsync();
                }
                var code = await Page.Locator(".docs-code-example pre code").First.InnerTextAsync();
                var snippet = new RenderedSnippet(route.Split('/').Last() + "-" + mixed, code,
                    [new("ButtonGroupThemePlayground.razor", route, "theme", "Razor · direct Radzen", code)]);
                var result = await compiler.CompileAsync(snippet);
                Assert.That(result.Status, Is.EqualTo("self-contained"), result.Output);
            }
        }
    }
}
