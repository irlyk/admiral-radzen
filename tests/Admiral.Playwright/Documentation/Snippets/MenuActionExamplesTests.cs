using Admiral.Playwright.Pages.Components.MenuAction;

namespace Admiral.Playwright.Documentation.Snippets;

[Category("Snippets")]
public sealed class MenuActionExamplesTests : GalleryTest
{
    [Test]
    public async Task AdmiralAndDirectRadzenExamplesCompileWithoutHostContextAsync()
    {
        await StartPageAsync("admiral", "en");
        var compiler = await SnippetCompiler.CreateAsync();
        foreach (var route in new[] { "/components/multi-button" })
        {
            foreach (var themeOnly in new[] { false, true })
            {
                var gallery = new MenuActionPage(Page, route, themeOnly);
                await gallery.InitializeAsync();
                var code = await Page.Locator(".docs-code-example pre code").First.InnerTextAsync();
                var snippet = new RenderedSnippet(route.Split('/').Last() + "-" + themeOnly, code,
                    [new("MenuActionPlayground.razor", route, themeOnly ? "theme" : "admiral", "Razor", code)]);
                var result = await compiler.CompileAsync(snippet);
                Assert.That(result.Status, Is.EqualTo("self-contained"), result.Output);
            }
        }
    }
}
