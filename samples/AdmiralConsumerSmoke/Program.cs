using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using Radzen.Admiral.Components;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddAdmiralComponents(options =>
{
    options.ThemeCookieName = "RadzenAdmiralTheme";
    options.ThemeCookieDuration = TimeSpan.FromDays(365);
});

var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("ru")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>()
    .Value);

app.UseStaticFiles();

app.UseRouting();

app.MapGet("/culture/{culture}", (string culture, string? redirectUri, string? theme, HttpContext context) =>
{
    if (!supportedCultures.Any(item => item.Name == culture))
    {
        return Results.BadRequest();
    }

    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });

    // The culture switch performs a full reload. Persist the active theme in this
    // response so the prerendered page uses the same theme even if the toggle's
    // asynchronous cookie update has not completed yet.
    if (theme is "admiral" or "admiral-dark")
    {
        context.Response.Cookies.Append("RadzenAdmiralTheme", theme, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(365),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    var target = string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri;
    return Results.LocalRedirect(target);
});

app.MapGet("/data/admiral-component-coverage.md", () =>
{
    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdmiralComponentCoverage.md");
    return stream is null
        ? Results.NotFound()
        : Results.Stream(stream, "text/markdown; charset=utf-8");
});

app.MapPost("/upload/smoke/success", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var files = form.Files.Select(file => new { file.FileName, file.Length, file.ContentType }).ToArray();
    return files.Length == 0
        ? Results.BadRequest(new { message = "No files were received." })
        : Results.Ok(new { files });
});

app.MapPost("/upload/smoke/error", () =>
    Results.Json(new { message = "Simulated upload failure." }, statusCode: StatusCodes.Status422UnprocessableEntity));

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
