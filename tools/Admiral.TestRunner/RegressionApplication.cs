using Admiral.TestRunner.Commands;
using Admiral.TestRunner.Execution;
using Admiral.TestRunner.Selection;

namespace Admiral.TestRunner;

internal sealed class RegressionApplication(RepositoryPaths paths)
{
    private readonly CommandExecutor _commands = new(paths.Root);

    public async Task<int> RunAsync(string[] arguments)
    {
        try
        {
            return await ExecuteAsync(RegressionCommand.Parse(arguments));
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine("Use --help to list available suites.");
            return 2;
        }
    }

    private async Task<int> ExecuteAsync(RegressionCommand command)
    {
        if (command.Name == "help")
        {
            Console.WriteLine("Commands: affected [--list] [files...], browser, all, install [browser...]");
            Console.WriteLine("Suites: " + string.Join(", ", SuiteCatalog.Names));
            return 0;
        }

        if (command.Name == "install")
        {
            return await _commands.RunAsync(
                "dotnet",
                ["run", "--project", paths.TestProject, "--", "install", .. command.InstallArguments]);
        }

        var filter = command.Name == "affected" ? await SelectAffectedFilterAsync(command.ChangedFiles) : SuiteCatalog.FilterFor(command.Name);
        if (command.Name == "affected" && filter is null)
        {
            Console.WriteLine("No browser-dependent source changes");
            return 0;
        }

        if (command.ListOnly)
        {
            Console.WriteLine(filter ?? "All tests");
            return 0;
        }

        if (SuiteCatalog.RequiresOnlyPrivateHost(command.Name))
        {
            Environment.SetEnvironmentVariable("SKIP_SMOKE_HOST", "1");
        }

        var testArguments = new List<string>
        {
            "test",
            paths.TestProject,
            "--logger",
            "trx",
            "--results-directory",
            paths.ResultsDirectory
        };
        if (filter is not null)
        {
            testArguments.AddRange(["--filter", filter]);
        }

        return await _commands.RunAsync("dotnet", testArguments);
    }

    private async Task<string?> SelectAffectedFilterAsync(string[] explicitFiles)
    {
        var changedFiles = explicitFiles;
        if (changedFiles.Length == 0)
        {
            var tracked = await _commands.CaptureAsync("git", ["diff", "--name-only", "HEAD"]);
            var untracked = await _commands.CaptureAsync("git", ["ls-files", "--others", "--exclude-standard"]);
            changedFiles = (tracked + "\n" + untracked).Split('\n', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
        }

        var selection = new AffectedSuiteSelector(paths).Select(changedFiles);
        Console.WriteLine(selection.FallbackReason is not null
            ? $"Affected fallback: complete suite ({selection.FallbackReason})"
            : "Affected suites: " + string.Join(", ", selection.Categories));
        return selection.Filter;
    }
}
