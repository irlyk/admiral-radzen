using System.Diagnostics;

namespace Admiral.TestRunner.Execution;

internal sealed class CommandExecutor(string workingDirectory)
{
    public async Task<int> RunAsync(string program, IEnumerable<string> arguments)
    {
        using var process = Start(program, arguments, capture: false);
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    public async Task<string> CaptureAsync(string program, IEnumerable<string> arguments)
    {
        using var process = Start(program, arguments, capture: true);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await error);
        }

        return await output;
    }

    private Process Start(string program, IEnumerable<string> arguments, bool capture)
    {
        var settings = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = capture,
            RedirectStandardError = capture
        };
        foreach (var argument in arguments)
        {
            settings.ArgumentList.Add(argument);
        }

        return Process.Start(settings) ?? throw new InvalidOperationException($"Could not start {program}");
    }
}
