namespace Admiral.Playwright.Infrastructure.Fixtures;

internal sealed record FontFixture(
    string Directory,
    string Family,
    string? ExpectedFamily,
    string[] Files,
    (int Weight, string File)[] Fonts)
{
    public static FontFixture? FromEnvironment(string family)
    {
        var directory = Environment.GetEnvironmentVariable("ADMIRAL_FONT_FIXTURE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        directory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
        Assert.That(
            directory != Repository.Root
                && !directory.StartsWith(Repository.Root + Path.DirectorySeparatorChar),
            Is.True,
            "Font fixture must stay outside the repository");
        var files = System.IO.Directory.GetFiles(directory).Select(Path.GetFileName).Cast<string>().Where(
            f => Regex.IsMatch(f, @"\.(otf|ttf|woff2?)$", RegexOptions.IgnoreCase)).ToArray();
        var fonts = new[]
        {
            (400, "Regular"),
            (500, "Medium"),
            (550, "SemiBold")
        }.Select(
            x => (x.Item1, files.FirstOrDefault(
                f => Regex.IsMatch(f, $@"(?:^|[-_]){x.Item2}(?:\.|[-_])", RegexOptions.IgnoreCase))!))
            .ToArray();
        Assert.That(fonts.All(f => f.Item2 != null), Is.True, "Fixture needs Regular, Medium and SemiBold");
        return new(
            directory,
            family,
            Environment.GetEnvironmentVariable("ADMIRAL_FONT_FIXTURE_EXPECTED_FAMILY"),
            files,
            fonts);
    }

    public string Css(string prefix) =>
        string.Join(
            "\n",
            Fonts.Select(
                f => $"@font-face{{font-family:'{Family}';src:url('./{prefix}{Uri.EscapeDataString(f.File)}');font-weight:{f.Weight};font-display:swap}}")) + $"\n:root{{--admiral-font-family:'{Family}',system-ui,sans-serif}}";
}
