using Admiral.TestRunner.Commands;

namespace Admiral.TestRunner.Tests.Commands;

public sealed class RegressionCommandTests
{
    [Test]
    public void EmptyArgumentsSelectAffected()
    {
        // Act & assert
        Assert.That(RegressionCommand.Parse([]).Name, Is.EqualTo("affected"));
    }

    [Test]
    public void AffectedPreservesExplicitFilesAndListOption()
    {
        // Act
        var command = RegressionCommand.Parse(["affected", "--list", "Components/Tree.razor"]);

        // Assert
        Assert.That(command.ListOnly, Is.True);
        Assert.That(command.ChangedFiles, Is.EqualTo(new[] { "Components/Tree.razor" }));
    }

    [Test]
    public void InstallForwardsBrowserArguments()
    {
        // Act
        var command = RegressionCommand.Parse(["install", "--with-deps", "chromium"]);

        // Assert
        Assert.That(command.InstallArguments, Is.EqualTo(new[] { "--with-deps", "chromium" }));
    }

    [Test]
    public void UnknownOptionsAreRejected()
    {
        // Act & assert
        Assert.Throws<ArgumentException>(() => RegressionCommand.Parse(["browser", "--unknown"]));
    }

    [TestCase("tree-select", "TreeSelect")]
    [TestCase("global-search", "GlobalSearch")]
    [TestCase("combo", "Combo")]
    public void SuiteAliasesSelectExpectedCategory(string suite, string category)
    {
        // Act & assert
        Assert.That(SuiteCatalog.FilterFor(suite), Is.EqualTo("TestCategory=" + category));
    }

    [Test]
    public void UnknownSuiteIsRejected()
    {
        // Act & assert
        Assert.Throws<ArgumentException>(() => SuiteCatalog.FilterFor("unknown"));
    }
}
