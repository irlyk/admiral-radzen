using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Admiral.Playwright.Infrastructure.Hosting;
// A plain static host intentionally returns 404.html with status 404 for deep
// links, matching project hosting on GitHub Pages rather than ASP.NET fallback.
internal sealed class StaticHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly string _directory;
    private readonly string _prefix;
    private readonly FontFixture? _fonts;
    public string Url { get; }

    public StaticHost(string directory, string prefix = "/", FontFixture? fonts = null)
    {
        _directory = Path.GetFullPath(directory);
        _prefix = prefix;
        _fonts = fonts;
        var origin = $"http://127.0.0.1:{NetworkPorts.GetAvailablePort()}";
        Url = origin + prefix;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(origin);
        _app = builder.Build();
        _app.Run(Respond);
        _app.StartAsync().GetAwaiter().GetResult();
    }

    private async Task Respond(HttpContext context)
    {
        var response = context.Response;
        response.Headers.AccessControlAllowOrigin = "*";
        response.Headers.CacheControl = "no-store";
        var pathname = Uri.UnescapeDataString(context.Request.Path.Value ?? "/");
        if (!pathname.StartsWith(_prefix, StringComparison.Ordinal))
        {
            response.StatusCode = 404;
            return;
        }

        var relative = pathname[_prefix.Length..];
        if (_fonts != null && relative is "font-opt-in.css" or "css/admiral-font-opt-in.css")
        {
            response.ContentType = "text/css";
            await response.WriteAsync(_fonts.Css(relative.StartsWith("css/") ? "fonts/" : ""));
            return;
        }

        var file = Path.GetFullPath(Path.Combine(_directory, relative.Length == 0 ? "index.html" : relative));
        if (_fonts != null)
        {
            var filename = Path.GetFileName(relative);
            if (_fonts.Files.Contains(filename)
                && (relative == filename || relative == "css/fonts/" + filename))
            {
                file = Path.Combine(_fonts.Directory, filename);
            }
        }

        if (!file.StartsWith(
            _directory + Path.DirectorySeparatorChar,
            StringComparison.Ordinal)
            && (_fonts == null
            || !file.StartsWith(
                _fonts.Directory + Path.DirectorySeparatorChar,
                StringComparison.Ordinal)))
        {
            response.StatusCode = 403;
            return;
        }

        if (!File.Exists(file))
        {
            file = Path.Combine(_directory, "404.html");
            response.StatusCode = 404;
        }

        if (!File.Exists(file))
        {
            return;
        }

        response.ContentType = Mime(Path.GetExtension(file));
        response.ContentLength = new FileInfo(file).Length;
        await response.SendFileAsync(file, context.RequestAborted);
    }

    private static string Mime(string extension) => extension.ToLowerInvariant() switch
    {
        ".html" => "text/html",
        ".js" => "text/javascript",
        ".wasm" => "application/wasm",
        ".json" => "application/json",
        ".css" => "text/css",
        ".svg" => "image/svg+xml",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        ".otf" => "font/otf",
        _ => "application/octet-stream"
    };

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
