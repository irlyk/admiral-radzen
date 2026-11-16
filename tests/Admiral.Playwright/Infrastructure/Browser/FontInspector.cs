namespace Admiral.Playwright.Infrastructure.Browser;

internal static class FontInspector
{
    public static async Task<IReadOnlyDictionary<string, string[]>> ReadRenderedFontsAsync(IPage page, string parentSelector)
    {
        var samples = new
        {
            latin = "Admiral",
            cyrillic = "Адмирал"
        };
        await page.EvaluateAsync(
            """
            ({ selector, samples }) => {
              const parent = selector.endsWith(':visible')
                ? [...document.querySelectorAll(selector.slice(0, -8))].find(
                    (element) => element.getClientRects().length,
                  )
                : document.querySelector(selector);
              if (!parent) throw new Error(`Missing font sample parent: ${selector}`);
              const style = getComputedStyle(parent);
              const container = document.createElement('span');
              container.dataset.admiralFontSamples = 'true';
              container.style.cssText = 'display:inline-flex;gap:2px;position:fixed;left:0;top:0;z-index:-1';
              container.style.fontFamily = style.fontFamily;
              container.style.fontWeight = style.fontWeight;
              container.style.fontStyle = style.fontStyle;
              container.style.fontSize = style.fontSize;
              for (const [script, value] of Object.entries(samples)) {
                const span = document.createElement('span');
                span.dataset.admiralScript = script;
                span.textContent = value;
                container.append(span);
              }
              document.body.append(container);
            }
            """,
            new { selector = parentSelector, samples = samples });
        await page.EvaluateAsync("() => document.fonts.ready");
        var session = await page.Context.NewCDPSessionAsync(page);
        try
        {
            await session.SendCommandAsync("DOM.enable");
            await session.SendCommandAsync("CSS.enable");
            var root = (await session.SendCommandAsync<DomDocumentResponse>("DOM.getDocument")).Root;
            var actual = new Dictionary<string, string[]>();
            foreach (var script in new[]
            {
                "latin",
                "cyrillic"
            })
            {
                var nodeId = (await session.SendCommandAsync<DomQueryResponse>(
                    "DOM.querySelector",
                    new { nodeId = root.NodeId, selector = $"[data-admiral-font-samples] [data-admiral-script=\"{script}\"]" })).NodeId;
                Assert.That(nodeId, Is.Not.Zero, $"CDP node for {script}");
                var fonts = (await session.SendCommandAsync<PlatformFontsResponse>("CSS.getPlatformFontsForNode", new { nodeId = nodeId })).Fonts;
                actual[script] = fonts.Where(font => font.GlyphCount > 0).Select(font => font.FamilyName).ToArray();
                Assert.That(actual[script].Length, Is.Not.Zero, $"{script} glyph uses a browser-selected font");
            }

            return actual;
        }
        finally
        {
            await session.DetachAsync();
            await page.EvaluateAsync("() => document.querySelector('[data-admiral-font-samples]')?.remove()");
        }
    }
}
