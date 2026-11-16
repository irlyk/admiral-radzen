namespace Admiral.Playwright.Infrastructure.Configuration;

internal static class Repository
{
    public static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Radzen.Admiral.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found");
    }
}
