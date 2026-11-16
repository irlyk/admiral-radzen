using Admiral.TestRunner.Selection;
using Admiral.TestRunner.Tests.Fixtures;

namespace Admiral.TestRunner.Tests.Selection;

public sealed class AffectedSuiteSelectorTests
{
    private TemporaryRepository _repository = null!;
    private AffectedSuiteSelector _selector = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new TemporaryRepository();
        _selector = new AffectedSuiteSelector(_repository.Paths);
        _repository.Write("tests/Admiral.Playwright/Components/Tree/TreeTests.cs", """
            [Category("Tree")]
            public class TreeTests
            {
                public void Case()
                {
                    var page = new TreePage();
                    var bounds = new ElementBounds();
                }
            }
            """);
        _repository.Write("tests/Admiral.Playwright/Pages/Components/Tree/TreePage.cs", """
            internal class TreePage { string Route = "/components/tree"; }
            """);
        _repository.Write("tests/Admiral.Playwright/Models/ElementBounds.cs", """
            internal record ElementBounds();
            """);
        _repository.Write("Radzen.AdmiralComponents/AdmiralTree.razor", "<div class=\"admiral-tree\" />");
        _repository.Write("Radzen.AdmiralComponents/AdmiralParent.razor", "<AdmiralTree />");
        _repository.Write("samples/AdmiralConsumerSmoke/Pages/Tree.razor", """
            @page "/components/tree"
            <AdmiralParent />
            """);
    }

    [TearDown]
    public void TearDown() => _repository.Dispose();

    [TestCase("tests/Admiral.Playwright/Components/Tree/TreeTests.cs")]
    [TestCase("tests/Admiral.Playwright/Pages/Components/Tree/TreePage.cs")]
    [TestCase("tests/Admiral.Playwright/Models/ElementBounds.cs")]
    public void NestedTestAndItsDependenciesSelectOwningCategory(string path)
    {
        // Act
        var selection = _selector.Select([path]);

        // Assert
        Assert.That(selection.Categories, Is.EqualTo(new[] { "Tree" }));
        Assert.That(selection.Filter, Is.EqualTo("TestCategory=Tree"));
        Assert.That(selection.FallbackReason, Is.Null);
    }

    [Test]
    public void ComponentCompositionIncludesBrowserAndPackageConsumers()
    {
        // Act
        var selection = _selector.Select(["Radzen.AdmiralComponents/AdmiralTree.razor"]);

        // Assert
        Assert.That(
            selection.Categories,
            Is.EquivalentTo(new[] { "Tree", "Audit", "Layout", "Documentation", "Consumer", "Wasm" }));
        Assert.That(selection.FallbackReason, Is.Null);
    }

    [Test]
    public void SharedScssSelectorsFallBackToCompleteSuite()
    {
        // Arrange
        var path = "Radzen.AdmiralThemes/themes/shared.scss";
        _repository.Write(path, ".rz-dropdown { color: red; }");

        // Assert
        var selection = _selector.Select([path]);

        Assert.That(selection.Filter, Does.Contain("TestCategory!=Radzen"));
        Assert.That(selection.FallbackReason, Is.Not.Null);
    }

    [TestCase("README.md")]
    [TestCase("docs/fonts.md")]
    public void DocumentationDoesNotRequireBrowserTests(string path)
    {
        // Act
        var selection = _selector.Select([path]);

        // Assert
        Assert.That(selection.HasTests, Is.False);
        Assert.That(selection.FallbackReason, Is.Null);
    }

    [TestCase("tools/Admiral.TestRunner/Program.cs")]
    [TestCase("tests/Admiral.Playwright/Infrastructure/Unknown.cs")]
    public void UnknownDependenciesFallBackToCompleteSuite(string path)
    {
        // Act
        var selection = _selector.Select([path]);

        // Assert
        Assert.That(selection.HasTests, Is.True);
        Assert.That(selection.FallbackReason, Is.Not.Null);
    }

    [Test]
    public void NoChangesSelectBaseline()
    {
        // Act & assert
        Assert.That(_selector.Select([]).FallbackReason, Is.Not.Null);
    }

    [Test]
    public void CategoryAttributeSupportsQualifiedNameAndWhitespace()
    {
        // Arrange
        var path = "tests/Admiral.Playwright/Components/Other/OtherTests.cs";
        _repository.Write(path, """
            [NUnit.Framework.CategoryAttribute ( "Other" )]
            public class OtherTests {}
            """);

        // Assert
        Assert.That(_selector.Select([path]).Categories, Is.EqualTo(new[] { "Other" }));
    }
}
