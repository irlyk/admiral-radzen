using System.Security.Cryptography;
using System.Text;

namespace Admiral.Playwright.Infrastructure.Browser;

internal sealed class FailureArtifacts(string directory)
{
    public async Task SaveScreenshotAsync(IPage page, int contextIndex)
    {
        var path = GetPath(contextIndex, ".png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
        TestContext.AddTestAttachment(path);
    }

    public string GetTracePath(int contextIndex) => GetPath(contextIndex, ".zip");

    private string GetPath(int contextIndex, string extension)
    {
        Directory.CreateDirectory(directory);
        var fullName = TestContext.CurrentContext.Test.FullName;
        var label = Regex.Replace(fullName, @"[^\w.-]", "_");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fullName)))[..8];
        return Path.Combine(directory, $"{label[..Math.Min(label.Length, 180)]}-{digest}-{contextIndex}{extension}");
    }
}
