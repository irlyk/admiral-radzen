namespace Admiral.Playwright.Documentation.Snippets;

[Category("Snippets")]
public sealed class CopyableRazorTests : BrowserTest
{
    [Test]
    public async Task RenderedExamplesCompileAgainstPackagesAsync()
    {
        // Arrange: create a documentation browser context.
        var (_, page) = await NewPageAsync(culture: "en", width: 1280, height: 900);

        // Act: collect the rendered Razor snippets.
        var inventory = await new SnippetScanner(page).ScanAsync();

        // Diagnostics: save the snippet inventory.
        await SaveReportAsync("inventory.json", inventory);

        // Assert: the documentation exposes compilable examples.
        Assert.That(inventory.Snippets, Is.Not.Empty, "No rendered examples captured");

        // Arrange: prepare an isolated package consumer for compilation.
        var compiler = await SnippetCompiler.CreateAsync();
        var results = new List<SnippetCompilation>();
        var limit = int.TryParse(Environment.GetEnvironmentVariable("SNIPPET_LIMIT"), out var value) ? value : 0;
        var snippets = limit > 0 ? inventory.Snippets.Take(limit) : inventory.Snippets;

        // Act: compile every selected snippet against the packaged components.
        foreach (var snippet in snippets)
        {
            var result = await compiler.CompileAsync(snippet);

            results.Add(result);
            TestContext.Progress.WriteLine($"{result.Id}: {result.Status}");
        }

        // Assert: collect compilation failures without hiding scan errors.
        var failures = results.Where(result => result.Failed).Select(
            result => $"{result.Id} {result.Occurrences[0].Route}: {result.Status}").ToArray();

        // Diagnostics: save the compilation report.
        await SaveReportAsync("results.json", new { inventory.ScanErrors, failures, results });

        // Assert: all scanned examples meet their compilation requirements.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(inventory.ScanErrors, Is.Empty);
            Assert.That(failures, Is.Empty);
        }
    }

    private async Task SaveReportAsync<T>(string filename, T report)
    {
        var output = Path.Combine(Output, "copyable-razor");
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, filename);
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path);
    }
}
