using System.Security.Cryptography;
using System.Xml.Linq;

namespace Admiral.Playwright.Infrastructure.Packages;

internal sealed record PackageConsumer(string Directory, string Project, string Publish, Dictionary<string, string> Environment)
{
    private static readonly string[] _packageProjects = ["Radzen.AdmiralComponents", "Radzen.AdmiralIcons", "Radzen.AdmiralThemes"];
    public static async Task<PackageConsumer> PrepareAsync(string label)
    {
        var directory = Path.Combine(Repository.Root, "artifacts", $"{label}-{Guid.NewGuid():N}");
        var cache = Path.Combine(directory, "nuget-packages");
        var consumer = new PackageConsumer(
            directory,
            Path.Combine(directory, "AdmiralConsumerValidation.csproj"),
            Path.Combine(directory, "publish"),
            new() { ["NUGET_PACKAGES"] = cache, ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1" });
        consumer.CopyTemplate();
        var configPath = consumer.WriteNuGetConfiguration();
        Assert.That(await File.ReadAllTextAsync(consumer.Project), Does.Not.Contain("ProjectReference"));
        await ProcessRunner.RunAsync(
            "dotnet",
            ["restore", consumer.Project, "--configfile", configPath, "--force", "-v:q"],
            environment: consumer.Environment);
        await consumer.VerifyRestoredPackagesAsync(cache);
        return consumer;
    }

    private void CopyTemplate()
    {
        System.IO.Directory.CreateDirectory(Directory);
        var source = Path.Combine(Repository.Root, "samples/AdmiralConsumerValidation");
        var files = System.IO.Directory.EnumerateFiles(source).Where(
            file => Path.GetExtension(file) is ".csproj" or ".cs" or ".razor");
        foreach (var file in files)
        {
            File.Copy(file, Path.Combine(Directory, Path.GetFileName(file)));
        }
    }

    private string WriteNuGetConfiguration()
    {
        var config = new XDocument(
            new XElement(
                "configuration",
                new XElement(
                    "packageSources",
                    new XElement("clear"),
                    new XElement(
                        "add",
                        new XAttribute("key", "local-admiral"),
                        new XAttribute(
                            "value",
                            Path.Combine(Repository.Root, "artifacts/packages/admiral"))),
                    new XElement(
                        "add",
                        new XAttribute("key", "nuget.org"),
                        new XAttribute("value", "https://api.nuget.org/v3/index.json")))));
        var path = Path.Combine(Directory, "NuGet.config");
        config.Save(path);
        return path;
    }

    private async Task VerifyRestoredPackagesAsync(string cache)
    {
        using var assets = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(Directory, "obj/project.assets.json")));
        var folders = assets.RootElement.GetProperty("packageFolders").EnumerateObject().Select(
            folder => Path.GetFullPath(folder.Name).TrimEnd(Path.DirectorySeparatorChar));
        Assert.That(folders, Is.EqualTo(new[] { cache }), "Consumer uses only its isolated NuGet cache");
        foreach (var packageProject in _packageProjects)
        {
            var xml = XDocument.Load(Path.Combine(Repository.Root, packageProject, packageProject + ".csproj"));
            var name = xml.Descendants("PackageId").First().Value;
            var version = xml.Descendants("Version").First().Value;
            var filename = $"{name.ToLowerInvariant()}.{version}.nupkg";
            var local = Path.Combine(Repository.Root, "artifacts/packages/admiral", $"{name}.{version}.nupkg");
            var restored = Path.Combine(cache, name.ToLowerInvariant(), version, filename);
            Assert.That(
                SHA256.HashData(await File.ReadAllBytesAsync(restored)),
                Is.EqualTo(SHA256.HashData(await File.ReadAllBytesAsync(local))),
                $"{name}: restored bytes differ from local package");
        }
    }
}
