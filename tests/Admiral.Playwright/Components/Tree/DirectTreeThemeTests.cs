using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Components.Tree;

[Category("Tree")]
public sealed class DirectTreeThemeTests : GalleryTest
{
    [TestCase("admiral", false, false)]
    [TestCase("admiral", true, false)]
    [TestCase("admiral", false, true)]
    [TestCase("admiral", true, true)]
    [TestCase("admiral-dark", false, false)]
    [TestCase("admiral-dark", true, false)]
    [TestCase("admiral-dark", false, true)]
    [TestCase("admiral-dark", true, true)]
    public async Task BranchAndLeafContentAlignAtSameLevelAsync(string theme, bool small, bool checks)
    {
        await StartPageAsync(theme, "en");
        await new GalleryPage(Page).NavigateAsync("/components/tree");
        await Page.Locator("[data-doc-tab=theme]").ClickAsync();
        var tree = Page.Locator(".rz-tree.admiral-tree");
        await Page.GetByLabel("Small", new() { Exact = true }).SetCheckedAsync(small);
        await Page.GetByLabel("AllowCheckBoxes", new() { Exact = true }).SetCheckedAsync(checks);
        var branch = tree.Locator(".rz-treenode-content").Filter(new() { Has = Page.GetByText("Reports", new() { Exact = true }) });
        var leaf = tree.Locator(".rz-treenode-content").Filter(new() { Has = Page.GetByText("Archive · Checkable=false", new() { Exact = true }) });
        await Assertions.Expect(branch).ToHaveCSSAsync("padding-inline-start", small ? "52px" : "56px");
        var branchLabel = await branch.Locator(".rz-treenode-label").GetBoundsAsync();
        var leafLabel = await leaf.Locator(".rz-treenode-label").GetBoundsAsync();
        Assert.That(leafLabel.X, Is.EqualTo(branchLabel.X).Within(1), "Sibling branch and leaf labels align");
        var rootBranch = tree.Locator(".rz-treenode-content[aria-level='1']").First;
        var rootLeaf = tree.Locator(".rz-treenode-content[aria-level='1']").Last;
        Assert.That((await rootLeaf.Locator(".rz-treenode-label").GetBoundsAsync()).X,
            Is.EqualTo((await rootBranch.Locator(".rz-treenode-label").GetBoundsAsync()).X).Within(1));
        if (checks)
        {
            Assert.That((await leaf.Locator(".rz-chkbox").GetBoundsAsync()).X,
                Is.EqualTo((await branch.Locator(".rz-chkbox").GetBoundsAsync()).X).Within(1));
        }

        await branch.Locator(".rz-tree-toggler").ClickAsync();
        await Assertions.Expect(tree.GetByText("Current month", new() { Exact = true })).ToBeHiddenAsync();
        await branch.Locator(".rz-tree-toggler").ClickAsync();
        await Assertions.Expect(tree.GetByText("Current month", new() { Exact = true })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Select current month", Exact = true }).ClickAsync();
        if (!checks)
        {
            await Assertions.Expect(tree.Locator(".rz-treenode-content-selected .rz-treenode-label"))
                .ToHaveTextAsync("Current month");
        }
        Assert.That((await leaf.Locator(".rz-treenode-label").GetBoundsAsync()).X,
            Is.EqualTo((await branch.Locator(".rz-treenode-label").GetBoundsAsync()).X).Within(1),
            "External selection does not change sibling indentation");
        await tree.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{small}-{checks}.png" });
    }
}
