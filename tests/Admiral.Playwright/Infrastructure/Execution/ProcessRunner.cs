using System.Diagnostics;

namespace Admiral.Playwright.Infrastructure.Execution;

internal static class ProcessRunner
{
    public static async Task<string> RunAsync(
        string program,
        IEnumerable<string> arguments,
        string? directory = null,
        IDictionary<string, string>? environment = null,
        bool requireSuccess = true)
    {
        var result = await RunWithResultAsync(program, arguments, directory, environment);
        if (requireSuccess && result.Code != 0)
        {
            throw new AssertionException($"{program} exited with {result.Code}:\n{result.Output}");
        }

        return result.Output;
    }

    public static async Task<(int Code, string Output)> RunWithResultAsync(
        string program,
        IEnumerable<string> arguments,
        string? directory = null,
        IDictionary<string, string>? environment = null)
    {
        using var process = Start(program, arguments, directory, environment);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }

            throw;
        }

        return (process.ExitCode, await stdout + await stderr);
    }

    public static Process Start(
        string program,
        IEnumerable<string> arguments,
        string? directory = null,
        IDictionary<string, string>? environment = null)
    {
        var info = new ProcessStartInfo(program)
        {
            WorkingDirectory = directory ?? Repository.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        info.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        // Keep build subprocesses from leaving nodes holding redirected output pipes.
        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        info.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        info.Environment["UseSharedCompilation"] = "false";
        foreach (var arg in arguments)
        {
            info.ArgumentList.Add(arg);
        }

        if (environment != null)
        {
            foreach (var pair in environment)
            {
                info.Environment[pair.Key] = pair.Value;
            }
        }

        return Process.Start(info) ?? throw new InvalidOperationException($"Could not start {program}");
    }
}
