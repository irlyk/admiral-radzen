namespace Admiral.Playwright.Documentation.Snippets;

internal sealed class SnippetCompiler
{
    private readonly PackageConsumer _consumer;
    private readonly string _snippetFile;

    private SnippetCompiler(PackageConsumer consumer)
    {
        _consumer = consumer;
        _snippetFile = Path.Combine(consumer.Directory, "ValidationSnippet.razor");
    }

    public static async Task<SnippetCompiler> CreateAsync()
    {
        var compiler = new SnippetCompiler(await PackageConsumer.PrepareAsync("snippets"));
        await File.WriteAllTextAsync(compiler._snippetFile, "<p>Package consumer baseline</p>\n");
        await ProcessRunner.RunAsync(
            "dotnet",
            ["build", compiler._consumer.Project, "--no-restore", "-nologo", "-v:q"],
            environment: compiler._consumer.Environment);
        return compiler;
    }

    public async Task<SnippetCompilation> CompileAsync(RenderedSnippet snippet)
    {
        if (RequiresHostContext(snippet))
        {
            return new SnippetCompilation(snippet.Id, "requires-host-context", snippet.Occurrences, [], [], [], "");
        }

        await File.WriteAllTextAsync(_snippetFile, snippet.Code + "\n");
        var built = await ProcessRunner.RunWithResultAsync(
            "dotnet",
            ["build", _consumer.Project, "--no-restore", "-nologo", "-v:q"],
            environment: _consumer.Environment);
        var diagnostics = ExtractDiagnostics(built.Output, "error");
        var warnings = ExtractDiagnostics(built.Output, "warning");
        var codes = diagnostics
            .Select(line => Regex.Match(line, @"\berror ((?:CS|RZ)\d+)\b").Groups[1].Value)
            .ToArray();
        var symbols = diagnostics
            .Where(line => Regex.IsMatch(line, @"\berror (?:CS0103|CS0246)\b"))
            .Select(line => Regex.Match(line, "[\"“'‘]([^\"”'’]+)[\"”'’]").Groups[1].Value)
            .Distinct()
            .ToArray();
        var missingContext = codes.Length > 0
            && codes.All(code => code is "CS0103" or "CS0246")
            && !symbols.Any(symbol => symbol is "True" or "False");
        var status = built.Code == 0
            ? warnings.Any(line => line.Contains("warning RZ10012")) ? "compiler-warning" : "self-contained"
            : missingContext ? "requires-component-context" : "compiler-failure";
        return new SnippetCompilation(
            snippet.Id,
            status,
            snippet.Occurrences,
            symbols,
            diagnostics,
            warnings,
            built.Output);
    }

    private static string[] ExtractDiagnostics(string output, string severity)
    {
        return output.Split('\n')
            .Where(line => Regex.IsMatch(line, $@"\b{severity} (?:CS|RZ)\d+\b"))
            .Distinct()
            .ToArray();
    }

    private static bool RequiresHostContext(RenderedSnippet snippet)
    {
        return Regex.IsMatch(snippet.Occurrences[0].Title,
                @"Program\.cs|Host page|Theme and icon assets", RegexOptions.IgnoreCase)
            || Regex.IsMatch(snippet.Code, @"^\s*(<link\b|<script\b|builder\.|using Microsoft\.)");
    }
}
