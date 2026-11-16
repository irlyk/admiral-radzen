using System.Diagnostics;

namespace Admiral.Playwright.Infrastructure.Hosting;

internal sealed class AppHost : IAsyncDisposable
{
    public string Url { get; }

    private readonly Process? _process;
    private readonly StaticHost? _static;
    private readonly Task<string>? _stdout;
    private readonly Task<string>? _stderr;

    private AppHost(string url, Process process)
    {
        Url = url;
        _process = process;
        _stdout = process.StandardOutput.ReadToEndAsync();
        _stderr = process.StandardError.ReadToEndAsync();
    }

    private AppHost(StaticHost host)
    {
        Url = host.Url;
        _static = host;
    }

    public static async Task<AppHost> StartAsync(string dll, string? directory = null)
    {
        var url = $"http://127.0.0.1:{NetworkPorts.GetAvailablePort()}";
        var process = ProcessRunner.Start(
            "dotnet",
            [dll],
            directory,
            new Dictionary<string, string> { ["ASPNETCORE_URLS"] = url, ["ASPNETCORE_ENVIRONMENT"] = "Development" });
        var host = new AppHost(url, process);
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
        try
        {
            for (var attempt = 0; attempt < 150; attempt++)
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"App exited early:\n{await host._stdout!}\n{await host._stderr!}");
                }

                try
                {
                    if ((await client.GetAsync(url)).IsSuccessStatusCode)
                    {
                        return host;
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                await Task.Delay(200);
            }

            throw new TimeoutException($"App did not become ready: {dll}");
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }
    }

    public static async Task<AppHost> StartConsumerAsync()
    {
        var project = await PackageConsumer.PrepareAsync("consumer");
        await ProcessRunner.RunAsync(
            "dotnet",
            ["publish", project.Project, "--no-restore", "-o", project.Publish],
            environment: project.Environment);
        return await StartAsync(Path.Combine(project.Publish, "AdmiralConsumerValidation.dll"), project.Publish);
    }

    private static string? _wasmWeb;

    public static async Task<AppHost> StartWasmAsync()
    {
        if (_wasmWeb == null)
        {
            var output = Path.Combine(Repository.Root, "artifacts/wasm-docs-publish");
            var cache = Path.Combine(Repository.Root, "artifacts/wasm-docs-nuget-cache");
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }

            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, true);
            }

            await ProcessRunner.RunAsync(
                "dotnet",
                ["publish", "samples/AdmiralDocs/AdmiralDocs.csproj", "-c", "Release", "-o", output],
                environment: new Dictionary<string, string> { ["NUGET_PACKAGES"] = cache });
            _wasmWeb = Path.Combine(output, "wwwroot");
            foreach (var asset in new[]
            {
                "index.html",
                "404.html",
                ".nojekyll",
                "_framework/blazor.webassembly.js",
                "css/site.css",
                "css/docs.css",
                "AdmiralDocs.styles.css",
                "images/image-viewer-a.svg",
                "_content/Radzen.Admiral.Themes/css/admiral-base.css",
                "_content/Radzen.Admiral.Themes/css/admiral-dark-base.css",
                "_content/Radzen.Blazor/fonts/MaterialSymbolsOutlined.woff2",
                "_content/Radzen.Admiral.Icons/admiral-radzen-icons.css",
                "_content/Radzen.Admiral.Icons/admiral-icons/system/star-solid.svg"
            })
            {
                Assert.That(File.Exists(Path.Combine(_wasmWeb, asset)), Is.True, $"Missing WASM asset: {asset}");
            }

            Assert.That(
                await File.ReadAllBytesAsync(Path.Combine(_wasmWeb, "css/site.css")),
                Is.EqualTo(
                    await File.ReadAllBytesAsync(
                        Path.Combine(
                            Repository.Root,
                            "samples/AdmiralConsumerSmoke/wwwroot/css/site.css"))));
            Assert.That(
                await File.ReadAllTextAsync(Path.Combine(_wasmWeb, "index.html")),
                Does.Match("<base\\s+href=\"/admiral-radzen/\""));
        }

        return new AppHost(
            new StaticHost(
                _wasmWeb,
                "/admiral-radzen/",
                FontFixture.FromEnvironment("Admiral WASM Fixture")));
    }

    public async ValueTask DisposeAsync()
    {
        if (_static != null)
        {
            await _static.DisposeAsync();
        }

        if (_process != null)
        {
            if (!_process.HasExited)
            {
                _process.Kill(true);
            }

            await _process.WaitForExitAsync();
            var log = await _stdout! + await _stderr!;
            var path = Path.Combine(Repository.Root, "artifacts/playwright/app-" + _process.Id + ".log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, log);
            _process.Dispose();
        }
    }
}
