using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Documentation.Snippets;

[Category("Snippets")]
public sealed class LayoutGridExamplesTests : GalleryTest
{
    [Test]
    public async Task LayoutAndGridExamplesCompileWithPackagedSlotsAndDimensionsAsync()
    {
        await StartPageAsync("admiral", "en");
        var compiler = await SnippetCompiler.CreateAsync();
        var gallery = new GalleryPage(Page);
        foreach (var route in new[] { "/components/layout", "/components/grid" })
        {
            foreach (var tab in new[] { "admiral", "api", "theme" })
            {
                await gallery.NavigateAsync(route);
                var button = Page.Locator($"[data-doc-tab={tab}]");
                await button.ClickAsync();
                await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
                var code = await Page.Locator(".docs-code-example pre code").First.InnerTextAsync();
                var snippet = new RenderedSnippet(route.Split('/').Last() + "-" + tab, code,
                    [new(route.Split('/').Last() + ".razor", route, tab, "Razor", code)]);
                var result = await compiler.CompileAsync(snippet);
                Assert.That(result.Status, Is.EqualTo("self-contained"), result.Output);
            }
        }
    }
}
