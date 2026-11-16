namespace Admiral.Playwright.Documentation.Snippets;

internal sealed record DocumentationSource(string File, string Source)
{
    public string[] Routes { get; } = Regex.Matches(Source, "@page\\s+\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();
}
