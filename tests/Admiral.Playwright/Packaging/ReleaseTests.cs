using System.IO.Compression;
using System.Xml.Linq;

namespace Admiral.Playwright.Packaging;

[Category("Release")]
public sealed class ReleaseTests
{
    [Test]
    public async Task PackagesContainExpectedAssetsAndLicensesAsync()
    {
        // Arrange
        var root = Repository.Root;

        // Assert
        foreach (var file in new[]
        {
            "README.md",
            "README.en.md",
            "LICENSE",
            "THIRD_PARTY_NOTICES.md"
        })
        {
            // Assert
            Assert.That(File.Exists(Path.Combine(root, file)), Is.True, $"Missing {file}");
        }

        // Assert
        var tracked = (await ProcessRunner.RunAsync("git", ["ls-files", "-z"])).Split('\0', StringSplitOptions.RemoveEmptyEntries);

        Assert.That(
            tracked.Where(
                name => Regex.IsMatch(
                    name,
                    @"(^|/)(?:bin|obj|node_modules|artifacts|test-results|playwright-report)(/|$)",
                    RegexOptions.IgnoreCase)
                    || Regex.IsMatch(name, @"(^|/)\.env(?:\..*)?$", RegexOptions.IgnoreCase)
                    && !name.EndsWith(".env.example")
                    || Regex.IsMatch(
                        name,
                        @"\.(?:pfx|p12|pem|key|nupkg|snupkg)$",
                        RegexOptions.IgnoreCase)),
            Is.Empty,
            "Tracked build output or sensitive files");

        // Act
        await ProcessRunner.RunAsync("git", ["diff", "--check"]);

        // Assert
        foreach (var (id, directory) in new[]
        {
            ("AdmiralRadzen.Icons", "Radzen.AdmiralIcons"),
            ("AdmiralRadzen.Themes", "Radzen.AdmiralThemes"),
            ("AdmiralRadzen.Components", "Radzen.AdmiralComponents")
        })
        {
            var project = XDocument.Load(Path.Combine(root, directory, directory + ".csproj"));
            string Value(string name) => project.Descendants(name).First().Value;

            // Assert
            Assert.That(Value("PackageId"), Is.EqualTo(id));

            var version = Value("Version");

            Assert.That(version, Is.Not.Empty);
            Assert.That(Value("PackageLicenseExpression"), Is.EqualTo("MIT"));

            using var package = ZipFile.OpenRead(
                Path.Combine(root, "artifacts/packages/admiral", $"{id}.{version}.nupkg"));

            // Assert
            var members = package.Entries.Select(e => e.FullName).ToArray();

            // Assert
            foreach (var name in new[]
            {
                "README.md",
                "LICENSE",
                "THIRD_PARTY_NOTICES.md",
                id + ".nuspec"
            })
            {
                // Assert
                Assert.That(members, Does.Contain(name));
            }

            string Member(string name)
            {
                using var reader = new StreamReader(package.GetEntry(name)!.Open());
                return reader.ReadToEnd();
            }

            // Assert
            Assert.That(Member("LICENSE"), Is.EqualTo(await File.ReadAllTextAsync(Path.Combine(root, "LICENSE"))));
            Assert.That(
                Member("THIRD_PARTY_NOTICES.md"),
                Is.EqualTo(
                    await File.ReadAllTextAsync(Path.Combine(root, "THIRD_PARTY_NOTICES.md"))));

            var nuspec = XDocument.Parse(Member(id + ".nuspec"));

            // Assert
            var ns = nuspec.Root!.Name.Namespace;

            Assert.That(nuspec.Descendants(ns + "id").First().Value, Is.EqualTo(id));
            Assert.That(nuspec.Descendants(ns + "version").First().Value, Is.EqualTo(version));
            Assert.That(nuspec.Descendants(ns + "license").First().Value, Is.EqualTo("MIT"));
            Assert.That(nuspec.Descendants(ns + "license").First().Attribute("type")?.Value, Is.EqualTo("expression"));
            Assert.That(
                members.Where(
                    name => Regex.IsMatch(name, @"\.(woff2?|ttf|otf|eot|ttc)$", RegexOptions.IgnoreCase)),
                Is.Empty);
            if (id == "AdmiralRadzen.Icons")
            {
                foreach (var member in new[]
                {
                    "staticwebassets/admiral-radzen-icons.css",
                    "staticwebassets/admiral-radzen-icons.js",
                    "staticwebassets/admiral-icons/icons.txt"
                })
                {
                    Assert.That(members, Does.Contain(member));
                }
            }
        }
    }

    [Test, Category("CleanRelease")]
    public async Task CheckoutHasNoUncommittedChangesAsync()
    {
        // Act & assert
        if (Environment.GetEnvironmentVariable("REQUIRE_CLEAN_RELEASE") != "1")
        {
            Assert.Ignore("Set REQUIRE_CLEAN_RELEASE=1 in a clean release checkout");
        }

        // Assert
        Assert.That(
            (await ProcessRunner.RunAsync("git", ["status", "--porcelain=v1", "--untracked-files=normal"])).Trim(),
            Is.Empty,
            "Release checkout must be clean");
    }
}
