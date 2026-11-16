namespace Admiral.TestRunner;

internal sealed record RepositoryPaths(string Root)
{
    public string TestDirectory => Path.Combine(Root, "tests/Admiral.Playwright");
    public string TestProject => Path.Combine(TestDirectory, "Admiral.Playwright.csproj");
    public string ResultsDirectory => Path.Combine(Root, "artifacts/test-results");

    public static RepositoryPaths Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Radzen.Admiral.slnx")))
        {
            directory = directory.Parent;
        }

        return new RepositoryPaths(
            directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found"));
    }
}
