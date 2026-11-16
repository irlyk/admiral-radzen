using System.Security.Cryptography;
using System.Text;

namespace Admiral.Playwright.Documentation.Snippets;

internal sealed class SnippetScanner(IPage page)
{
    public async Task<SnippetInventory> ScanAsync()
    {
        var occurrences = new List<SnippetOccurrence>();
        var scanErrors = new List<string>();
        var pages = Directory.EnumerateFiles(
            Path.Combine(Repository.Root, "samples/AdmiralConsumerSmoke/Pages"),
            "*.razor")
            .Order()
            .Select(file => new DocumentationSource(Path.GetFileName(file), File.ReadAllText(file)))
            .Where(source => source.Routes.Length > 0)
            .ToArray();
        async Task CaptureAsync(string file, string route, string tab)
        {
            var examples = await page.Locator(".docs-code-example").ReadSnapshotsAsync<RenderedExample>(
                "elements => elements.map(e => ({ title: e.querySelector('h3')?.textContent?.trim() || 'Razor', code: e.querySelector('pre code')?.textContent || '' }))");
            foreach (var example in examples)
            {
                var code = example.Code;
                if (!string.IsNullOrWhiteSpace(code))
                {
                    occurrences.Add(new(file, route, tab, example.Title, code));
                }
            }
        }

        foreach (var entry in pages)
        {
            var route = entry.Routes[0];
            try
            {
                var response = await page.GotoAsync(
                    TestSettings.SmokeUrl + route,
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                Assert.That(response?.Status, Is.EqualTo(200), route);
                await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();
                await CaptureAsync(entry.File, route, "initial");
                var tabs = await page.Locator(".component-doc-tabs [data-doc-tab]").EvaluateAllAsync<string[]>("elements => elements.map(e => e.getAttribute('data-doc-tab'))");
                foreach (var tab in tabs)
                {
                    await page.Locator($".component-doc-tabs [data-doc-tab=\"{tab}\"]").First.ClickAsync();
                    await page.WaitForFunctionAsync(
                        "tab => document.querySelector(`.component-doc-tabs [data-doc-tab=\"${tab}\"]`)?.getAttribute('aria-pressed') === 'true'",
                        tab);
                    await CaptureAsync(entry.File, route, tab);
                }
            }
            catch (Exception error)
            {
                scanErrors.Add($"{route}: {error.Message}");
            }
        }

        foreach (var entry in pages.Where(e => e.Source.Contains("<CopyableRazorExample")))
        {
            if (!occurrences.Any(o => o.File == entry.File))
            {
                scanErrors.Add($"{entry.File}: source declares examples but none rendered");
            }
        }

        var snippets = occurrences
            .GroupBy(
                occurrence => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(occurrence.Code)))[..12])
            .Select(group => new RenderedSnippet(group.Key, group.First().Code, group.ToArray()))
            .ToArray();
        return new SnippetInventory(pages, scanErrors.ToArray(), occurrences.ToArray(), snippets);
    }
}
