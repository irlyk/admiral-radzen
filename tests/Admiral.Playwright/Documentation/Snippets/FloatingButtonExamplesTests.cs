using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Documentation.Snippets;

[Category("Snippets")]
public sealed class FloatingButtonExamplesTests : GalleryTest
{
    [Test]
    public async Task StandaloneAndMenuExamplesCompileWithoutHostStateAsync()
    {
        await StartPageAsync("admiral", "en");
        var compiler = await SnippetCompiler.CreateAsync();
        var gallery = new GalleryPage(Page);
        foreach (var route in new[] { "/components/floating-button", "/components/floating-button-menu" })
        {
            foreach (var tab in new[] { "admiral", "theme" })
            {
                await gallery.NavigateAsync(route);
                var selected = Page.Locator($"[data-doc-tab={tab}]");
                await selected.ClickAsync();
                await Assertions.Expect(selected).ToHaveAttributeAsync("aria-pressed", "true");
                var code = Page.Locator(".docs-code-example pre code").First;
                await Assertions.Expect(code).ToContainTextAsync(tab == "theme" ? "<Radzen" : "<Admiral");
                var text = await code.InnerTextAsync();
                var snippet = new RenderedSnippet(route.Split('/').Last() + "-" + tab, text,
                    [new(route.Split('/').Last() + ".razor", route, tab, "Razor", text)]);
                var result = await compiler.CompileAsync(snippet);
                Assert.That(result.Status, Is.EqualTo("self-contained"), result.Output);
            }
        }
    }
}
