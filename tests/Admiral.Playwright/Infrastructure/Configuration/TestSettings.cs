namespace Admiral.Playwright.Infrastructure.Configuration;

internal static class TestSettings
{
    public static string? ExistingSmokeUrl => Environment.GetEnvironmentVariable("SMOKE_URL");
    public static string SmokeUrl => ExistingSmokeUrl ?? "http://127.0.0.1:5072";
    public static string ArtifactRoot =>
        Path.GetFullPath(
            Environment.GetEnvironmentVariable("AUDIT_OUTPUT") ?? Path.Combine(Repository.Root, "artifacts/playwright"));
    public static bool Headless => Environment.GetEnvironmentVariable("HEADED") != "1";
    public static string? ChromiumPath => Environment.GetEnvironmentVariable("CHROME_PATH");

    public const int ElementTimeoutMs = 8000;
}
