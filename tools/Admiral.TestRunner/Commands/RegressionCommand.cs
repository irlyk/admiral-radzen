namespace Admiral.TestRunner.Commands;

internal sealed record RegressionCommand(string Name, bool ListOnly, string[] ChangedFiles, string[] InstallArguments)
{
    public static RegressionCommand Parse(string[] arguments)
    {
        var hasCommand = arguments.Length > 0 && !arguments[0].StartsWith('-');
        var name = hasCommand ? arguments[0] : arguments.Contains("--help") ? "help" : "affected";
        var remaining = hasCommand ? arguments[1..] : arguments;
        if (name == "install")
        {
            return new RegressionCommand(name, false, [], remaining.Length == 0 ? ["chromium"] : remaining);
        }

        var unknown = remaining.FirstOrDefault(
            argument => argument.StartsWith('-') && argument != "--list" && argument != "--help");
        if (unknown is not null)
        {
            throw new ArgumentException($"Unknown option: {unknown}");
        }

        return new RegressionCommand(
            name,
            remaining.Contains("--list"),
            remaining.Where(argument => !argument.StartsWith('-')).ToArray(),
            []);
    }
}
