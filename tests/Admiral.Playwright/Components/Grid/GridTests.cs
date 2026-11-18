using Admiral.Playwright.Pages;
using Admiral.Playwright.Pages.Components.Grid;

namespace Admiral.Playwright.Components.Grid;

[Category("Layout")]
public sealed class GridTests : GalleryTest
{
    public static IEnumerable<TestCaseData> Breakpoints()
    {
        foreach (var theme in new[] { "admiral", "admiral-dark" })
        foreach (var native in new[] { false, true })
        foreach (var width in new[] { 320, 767, 768, 1023, 1024, 1279, 1280, 1600 })
            yield return new TestCaseData(theme, native, width);
    }

    [TestCaseSource(nameof(Breakpoints))]
    public async Task SpansGapsWrappingAndAllItemsRemainVisibleAsync(string theme, bool native, int width)
    {
        await StartPageAsync(theme, "en", width, 1000);
        var sample = new GridPage(Page, native);
        await sample.InitializeAsync();
        var snapshot = await sample.MeasureAsync();
        var columns = native ? 12 : width >= 1024 ? 12 : width >= 768 ? 8 : 4;
        var span = native ? 4 : width >= 1024 ? 4 : 2;
        var gap = native ? 16 : width >= 1280 ? 32 : width >= 768 ? 24 : 16;
        Assert.That(snapshot.Columns, Is.EqualTo(columns));
        Assert.That(snapshot.Gap, Is.EqualTo(gap));
        Assert.That(snapshot.Padding, Is.EqualTo(native ? 0 : width >= 1600 ? 144 : width >= 1280 ? 128 : width >= 768 ? 64 : 16));
        var cellWidth = (snapshot.ContentWidth - (columns - 1) * gap) / columns;
        var expected = cellWidth * span + (span - 1) * gap;
        foreach (var item in snapshot.Items)
        {
            Assert.That(item.Width, Is.EqualTo(expected).Within(1));
            Assert.That(item.Left, Is.GreaterThanOrEqualTo(snapshot.Root.Left - 1));
            Assert.That(item.Right, Is.LessThanOrEqualTo(snapshot.Root.Right + 1));
        }
        Assert.That(snapshot.Items[1].Left - snapshot.Items[0].Right, Is.EqualTo(gap).Within(1));
        var columnsPerRow = columns / span;
        Assert.That(snapshot.Items[columnsPerRow].Top, Is.GreaterThan(snapshot.Items[0].Top));
        for (var i = 0; i < 6; i++)
        {
            await Assertions.Expect(sample.Items.Nth(i)).ToBeVisibleAsync();
            await Assertions.Expect(sample.Items.Nth(i)).ToHaveTextAsync((i + 1).ToString());
        }
        if (!native)
        {
            await sample.SetAlternativeAsync(true);
            await Assertions.Expect(sample.Grid).ToHaveClassAsync(new Regex("--alternative"));
            snapshot = await sample.MeasureAsync();
            Assert.That(snapshot.Padding, Is.EqualTo(width >= 1024 ? 32 : width >= 768 ? 24 : 16));
            Assert.That(snapshot.Gap, Is.EqualTo(width >= 1024 ? 32 : width >= 768 ? 24 : 16));
            await sample.Grid.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{width}-alternative.png" });
        }
    }

    [TestCase("admiral", 390, "MobileSpan", 4)]
    [TestCase("admiral", 900, "TabletSpan", 8)]
    [TestCase("admiral", 1440, "DesktopSpan", 12)]
    [TestCase("admiral-dark", 390, "MobileSpan", 4)]
    [TestCase("admiral-dark", 900, "TabletSpan", 8)]
    [TestCase("admiral-dark", 1440, "DesktopSpan", 12)]
    public async Task InvalidSpansAreClampedWithoutHidingItemsAsync(string theme, int width, string parameter, int maximum)
    {
        await StartPageAsync(theme, "en", width, 1000);
        var sample = new GridPage(Page);
        await sample.InitializeAsync();
        foreach (var (input, span) in new[] { ("-5", 1), ("99", maximum) })
        {
            await sample.SetAsync(parameter, input);
            await Assertions.Expect(sample.Items.First).ToHaveCSSAsync("grid-column-start", "span " + span);
            await Assertions.Expect(sample.Items).ToHaveCountAsync(6);
            await Assertions.Expect(sample.Items.Last).ToBeVisibleAsync();
        }
    }

    [TestCase("admiral", 390)]
    [TestCase("admiral", 900)]
    [TestCase("admiral", 1440)]
    [TestCase("admiral-dark", 390)]
    [TestCase("admiral-dark", 900)]
    [TestCase("admiral-dark", 1440)]
    public async Task IntegrationRulersOwnTheirGridAndDoNotOverflowAsync(string theme, int width)
    {
        await StartPageAsync(theme, "en", width, 1000);
        await new GalleryPage(Page).NavigateAsync("/layout");
        var rulers = Page.Locator(".admiral-grid-guide");
        await Assertions.Expect(rulers).ToHaveCountAsync(2);
        for (var i = 0; i < 2; i++)
        {
            await Assertions.Expect(rulers.Nth(i)).ToHaveCSSAsync("display", "grid");
            await Assertions.Expect(rulers.Nth(i).Locator(".admiral-grid-guide__column:visible")).ToHaveCountAsync(width >= 1024 ? 12 : width >= 768 ? 8 : 4);
            var bounds = await rulers.Nth(i).GetBoundsAsync();
            Assert.That(bounds.X, Is.GreaterThanOrEqualTo(0));
            Assert.That(bounds.X + bounds.Width, Is.LessThanOrEqualTo(width));
            var columns = rulers.Nth(i).Locator(".admiral-grid-guide__column:visible");
            Assert.That((await columns.First.GetBoundsAsync()).Width, Is.GreaterThan(0));
            var last = await columns.Last.GetBoundsAsync();
            Assert.That(last.X + last.Width, Is.LessThanOrEqualTo(bounds.X + bounds.Width + 1));
        }
        await Page.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{width}-integration.png", FullPage = true });
    }
}
