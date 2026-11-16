namespace Admiral.Playwright;

[SetUpFixture]
public class TestEnvironment
{
    private AppHost? _smoke;

    [OneTimeSetUp]
    public async Task StartSmokeWhenNeededAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMOKE_URL")))
        {
            return;
        }

        var root = Repository.Root;
        if (OperatingSystem.IsWindows())
        {
            await ProcessRunner.RunAsync(
                "powershell",
                ["-NoProfile", "-File", "tools/admiral/build-admiral-packages.ps1", "-Configuration", "Release"]);
        }
        else
        {
            await ProcessRunner.RunAsync(
                "bash",
                ["tools/admiral/build-admiral-packages.sh", "--configuration", "Release"]);
        }

        if (Environment.GetEnvironmentVariable("SKIP_SMOKE_HOST") == "1")
        {
            return;
        }

        var output = Path.Combine(root, "artifacts/playwright-smoke-publish");
        var cache = Path.Combine(root, "artifacts/playwright-smoke-cache");
        if (Directory.Exists(cache))
        {
            Directory.Delete(cache, true);
        }

        await ProcessRunner.RunAsync(
            "dotnet",
            ["publish", "samples/AdmiralConsumerSmoke/AdmiralConsumerSmoke.csproj", "-c", "Release", "-o", output, "--force"],
            environment: new Dictionary<string, string> { ["NUGET_PACKAGES"] = cache });
        _smoke = await AppHost.StartAsync(Path.Combine(output, "AdmiralConsumerSmoke.dll"), output);
        Environment.SetEnvironmentVariable("SMOKE_URL", _smoke.Url);
    }

    [OneTimeTearDown]
    public async Task StopOwnedSmokeAsync()
    {
        if (_smoke != null)
        {
            await _smoke.DisposeAsync();
            Environment.SetEnvironmentVariable("SMOKE_URL", null);
        }
    }
}
