using Admiral.Playwright.Pages.Components.Layout;

namespace Admiral.Playwright.Components.Layout;

[Category("Layout")]
public sealed class LayoutTests : GalleryTest
{
    public static IEnumerable<TestCaseData> Compositions()
    {
        foreach (var theme in new[] { "admiral", "admiral-dark" })
        {
            foreach (var native in new[] { false, true })
            {
                foreach (var width in new[] { 390, 900, 1100, 1440 })
                {
                    foreach (var composition in new[] { "body", "header-body", "header-sidebar-body", "sidebar-body", "header-sidebar-body-footer", "sidebar-body-footer", "body-footer" })
                    {
                        yield return new TestCaseData(theme, native, width, composition);
                    }
                }
            }
        }
    }

    [TestCaseSource(nameof(Compositions))]
    public async Task OptionalSectionsFillTheLayoutWithoutGapsOrOverlapAsync(string theme, bool native, int width, string composition)
    {
        await StartPageAsync(theme, "en", width, 1000);
        var sample = new LayoutPage(Page, native);
        await sample.InitializeAsync(composition);
        var hasSidebar = composition.Contains("sidebar");
        if (!native && hasSidebar && width < 1280)
        {
            await sample.ToggleSidebarAsync();
            await Assertions.Expect(sample.Layout).ToHaveClassAsync(new Regex("--menu-open"));
            // The width/transform transition must settle before measuring its visible bounds.
            await sample.Sidebar.EvaluateAsync("async e => { await Promise.all(e.getAnimations().map(a => a.finished.catch(() => {}))); }");
        }
        if (native && hasSidebar)
        {
            await Assertions.Expect(sample.Sidebar).ToHaveCSSAsync("opacity", "1");
        }

        var geometry = await sample.MeasureAsync();
        Assert.That(geometry.Root.Height, Is.EqualTo(480).Within(1));
        Assert.That(geometry.Header is not null, Is.EqualTo(composition.Contains("header")));
        Assert.That(geometry.Footer is not null, Is.EqualTo(composition.Contains("footer")));
        Assert.That(geometry.Sidebar is not null, Is.EqualTo(hasSidebar));
        var bodyTop = geometry.Header?.Bottom ?? geometry.Root.Top;
        var bodyBottom = geometry.Footer?.Top ?? geometry.Root.Bottom;
        Assert.That(geometry.Body.Top, Is.EqualTo(bodyTop).Within(1));
        Assert.That(geometry.Body.Bottom, Is.EqualTo(bodyBottom).Within(1));
        var reservedSidebar = !hasSidebar || !native && width < 1024 ? 0 : !native && width < 1280 ? 56 : 288;
        Assert.That(geometry.Body.Left, Is.EqualTo(geometry.Root.Left + reservedSidebar).Within(1));
        Assert.That(geometry.Body.Right, Is.EqualTo(geometry.Root.Right).Within(1));
        foreach (var section in new[] { geometry.Header, geometry.Footer }.Where(x => x is not null))
        {
            Assert.That(section!.Left, Is.EqualTo(geometry.Root.Left).Within(1));
            Assert.That(section.Right, Is.EqualTo(geometry.Root.Right).Within(1));
        }
        if (geometry.Header is not null)
        {
            Assert.That(geometry.Header.Height, Is.EqualTo(64).Within(1));
        }

        if (geometry.Footer is not null)
        {
            Assert.That(geometry.Footer.Height, Is.EqualTo(56).Within(1));
        }

        if (geometry.Sidebar is not null)
        {
            Assert.That(geometry.Sidebar.Top, Is.EqualTo(bodyTop).Within(1));
            Assert.That(geometry.Sidebar.Bottom, Is.EqualTo(bodyBottom).Within(1));
            Assert.That(geometry.Sidebar.Left, Is.EqualTo(geometry.Root.Left).Within(1));
            Assert.That(geometry.Sidebar.Width, Is.EqualTo(!native && width < 1024 ? Math.Min(288, geometry.Root.Width - 56) : 288).Within(1));
        }
        if (composition == "header-sidebar-body-footer")
        {
            await sample.Layout.ScreenshotAsync(new() { Path = $"{Output}/{theme}-{native}-{width}-all-sections.png" });
        }
    }

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task CustomHeaderSidebarFooterLengthsAndBodyScrollingAsync(string theme, bool native)
    {
        await StartPageAsync(theme, "en", 1440, 1000);
        var sample = new LayoutPage(Page, native);
        await sample.InitializeAsync("header-sidebar-body-footer");
        foreach (var (css, pixels) in new[] { ("48px", 48), ("80px", 80), ("6rem", 96), ("calc(2rem + 24px)", 56) })
        {
            await sample.SetLengthAsync("HeaderHeight", css);
            await Assertions.Expect(sample.Header).ToHaveCSSAsync("height", pixels + "px");
        }
        foreach (var (css, pixels) in new[] { ("160px", 160), ("22rem", 352), ("288px", 288) })
        {
            await sample.SetLengthAsync("SidebarWidth", css);
            await Assertions.Expect(sample.Sidebar).ToHaveCSSAsync("width", pixels + "px");
        }
        foreach (var (css, pixels) in new[] { ("40px", 40), ("4rem", 64), ("72px", 72) })
        {
            await sample.SetLengthAsync("FooterHeight", css);
            await Assertions.Expect(sample.Footer).ToHaveCSSAsync("height", pixels + "px");
        }
        await sample.SetTallAsync(true);
        var before = await sample.MeasureAsync();
        Assert.That(before.ScrollHeight, Is.GreaterThan(before.ClientHeight));
        await sample.Body.EvaluateAsync("e => e.scrollTop = e.scrollHeight");
        var after = await sample.MeasureAsync();
        Assert.That(after.Header, Is.EqualTo(before.Header));
        Assert.That(after.Footer, Is.EqualTo(before.Footer));
        Assert.That(await sample.Body.EvaluateAsync<double>("e => e.scrollTop"), Is.GreaterThan(0));
    }

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task GridItemsKeepTheirSpansInsideLayoutAndNavigationIsAbsentWithoutSidebarAsync(string theme)
    {
        await StartPageAsync(theme, "en", 1440, 1000);
        var sample = new LayoutPage(Page);
        await sample.InitializeAsync("header-body");
        await Assertions.Expect(sample.Header.Locator("button[aria-controls]")).ToHaveCountAsync(0);
        var grid = sample.Body.Locator(".admiral-grid").First;
        var first = await sample.Layout.GetByTestId("layout-panel-a").GetBoundsAsync();
        var second = await sample.Layout.GetByTestId("layout-panel-b").GetBoundsAsync();
        Assert.That(first.Y, Is.EqualTo(second.Y).Within(1));
        var width = await grid.EvaluateAsync<double>("e => e.clientWidth - parseFloat(getComputedStyle(e).paddingLeft) - parseFloat(getComputedStyle(e).paddingRight)");
        var gap = await grid.EvaluateAsync<double>("e => parseFloat(getComputedStyle(e).columnGap)");
        Assert.That(first.Width, Is.EqualTo((width - gap) / 2).Within(1));
        Assert.That(second.X - first.X - first.Width, Is.EqualTo(gap).Within(1));
    }
    [TestCase("admiral", 390)]
    [TestCase("admiral-dark", 390)]
    [TestCase("admiral", 1440)]
    [TestCase("admiral-dark", 1440)]
    public async Task DirectRadzenCollapsedSidebarDoesNotReserveAnInvisibleStripAsync(string theme, int width)
    {
        await StartPageAsync(theme, "en", width, 1000);
        var sample = new LayoutPage(Page, true);
        await sample.InitializeAsync("header-sidebar-body-footer");
        await sample.ToggleSidebarAsync();
        await Assertions.Expect(sample.Sidebar).ToHaveClassAsync(new Regex("rz-sidebar-collapsed"));
        await Assertions.Expect(sample.Sidebar).ToHaveAttributeAsync("inert", "");
        await sample.Sidebar.EvaluateAsync("async e => { await Promise.all(e.getAnimations().map(a => a.finished.catch(() => {}))); }");
        var collapsed = await sample.MeasureAsync();
        Assert.That(collapsed.Sidebar!.Width, Is.EqualTo(0).Within(0.1));
        Assert.That(collapsed.Body.Left, Is.EqualTo(collapsed.Root.Left).Within(0.1));
        await sample.ToggleSidebarAsync();
        await Assertions.Expect(sample.Sidebar).ToHaveClassAsync(new Regex("rz-sidebar-expanded"));
        await sample.Sidebar.EvaluateAsync("async e => { await Promise.all(e.getAnimations().map(a => a.finished.catch(() => {}))); }");
        Assert.That((await sample.MeasureAsync()).Sidebar!.Width, Is.EqualTo(288).Within(1));
    }

    [TestCase("admiral", 390)]
    [TestCase("admiral-dark", 390)]
    [TestCase("admiral", 1100)]
    [TestCase("admiral-dark", 1100)]
    public async Task ClosedMobileNavigationIsNotFocusableAndEscapeRestoresTriggerAsync(string theme, int width)
    {
        await StartPageAsync(theme, "en", width, 1000);
        var sample = new LayoutPage(Page);
        await sample.InitializeAsync("header-sidebar-body-footer");
        if (width < 1024)
        {
            await Assertions.Expect(sample.Sidebar).ToHaveCSSAsync("visibility", "hidden");
        }

        Assert.That(await sample.Layout.Locator(":scope > .admiral-layout__overlay").EvaluateAsync<int>("e => e.tabIndex"), Is.EqualTo(-1));
        var trigger = sample.Header.GetByRole(AriaRole.Button, new() { Name = "Open navigation", Exact = true });
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(sample.Sidebar).ToHaveCSSAsync("visibility", "visible");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await sample.Sidebar.EvaluateAsync<bool>("e => e.contains(document.activeElement)"), Is.True);
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
        if (width < 1024)
        {
            await Assertions.Expect(sample.Sidebar).ToHaveCSSAsync("visibility", "hidden");
        }
    }

    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task RtlSidebarAndFooterKeepTheCorrectEdgesAsync(string theme, bool native)
    {
        await StartPageAsync(theme, "en", 1440, 1000);
        var sample = new LayoutPage(Page, native);
        await sample.InitializeAsync("header-sidebar-body-footer");
        await sample.Controls.GetByLabel("RTL", new() { Exact = true }).CheckAsync();
        await Assertions.Expect(sample.Layout).ToHaveAttributeAsync("dir", "rtl");
        var geometry = await sample.MeasureAsync();
        Assert.That(geometry.Sidebar!.Right, Is.EqualTo(geometry.Root.Right).Within(1));
        Assert.That(geometry.Body.Left, Is.EqualTo(geometry.Root.Left).Within(1));
        Assert.That(geometry.Body.Right, Is.EqualTo(geometry.Sidebar.Left).Within(1));
        Assert.That(geometry.Footer!.Width, Is.EqualTo(geometry.Root.Width).Within(1));
    }

    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task NestedLayoutsHaveUniqueSidebarIdsAndDesktopModesResizeBodyAsync(string theme)
    {
        await StartPageAsync(theme, "en", 1440, 1000);
        var sample = new LayoutPage(Page);
        await sample.InitializeAsync("header-sidebar-body-footer");
        var ids = await Page.Locator(".admiral-layout__side-menu[id]").EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.id)");
        Assert.That(ids.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
        await Assertions.Expect(sample.Header.GetByRole(AriaRole.Button, new() { Name = "Toggle navigation", Exact = true }))
            .ToHaveAttributeAsync("aria-controls", await sample.Sidebar.GetAttributeAsync("id") ?? "");
        foreach (var (state, expected) in new[] { ("IconsOnly", 56), ("Hidden", 0), ("Expanded", 288) })
        {
            await sample.Controls.GetByLabel("Sidebar state", new() { Exact = true }).SelectOptionAsync(state);
            await Assertions.Expect(sample.Layout).ToHaveClassAsync(new Regex("--side-menu-" + state.ToLowerInvariant()));
            await sample.Sidebar.EvaluateAsync("async e => { await Promise.all(e.getAnimations().map(a => a.finished.catch(() => {}))); }");
            var geometry = await sample.MeasureAsync();
            Assert.That(geometry.Body.Left - geometry.Root.Left, Is.EqualTo(expected).Within(1));
            if (state == "Hidden")
            {
                await Assertions.Expect(sample.Sidebar).ToHaveCSSAsync("visibility", "hidden");
            }
        }
    }
    [TestCase("admiral")]
    [TestCase("admiral-dark")]
    public async Task RemovingSidebarClosesOverlayAndPlainBodyCanOptOutOfTheGridAsync(string theme)
    {
        await StartPageAsync(theme, "en", 390, 1000);
        var sample = new LayoutPage(Page);
        await sample.InitializeAsync("header-sidebar-body-footer");
        await sample.ToggleSidebarAsync();
        await Assertions.Expect(sample.Layout).ToHaveClassAsync(new Regex("--menu-open"));
        await sample.Controls.GetByLabel("HasSideMenu", new() { Exact = true }).UncheckAsync();
        await Assertions.Expect(sample.Sidebar).ToHaveCountAsync(0);
        await Assertions.Expect(sample.Header.Locator("button[aria-controls]")).ToHaveCountAsync(0);
        await Assertions.Expect(sample.Demo.Locator("output")).ToContainTextAsync("MenuOpen: False");
        await sample.Controls.GetByLabel("HasSideMenu", new() { Exact = true }).CheckAsync();
        await Assertions.Expect(sample.Sidebar).ToHaveCSSAsync("visibility", "hidden");
        await sample.Controls.GetByLabel("UseBodyGrid", new() { Exact = true }).UncheckAsync();
        await Assertions.Expect(sample.Body.Locator(".admiral-layout__body-grid")).ToHaveCSSAsync("display", "block");
        var content = sample.Body.GetByText("Body", new() { Exact = true });
        Assert.That((await content.GetBoundsAsync()).Width, Is.GreaterThan(200));
    }
    [TestCase("admiral", false)]
    [TestCase("admiral", true)]
    [TestCase("admiral-dark", false)]
    [TestCase("admiral-dark", true)]
    public async Task FooterAndSidebarTextRemainReadableAsync(string theme, bool native)
    {
        await StartPageAsync(theme, "en", 1440, 1000);
        var sample = new LayoutPage(Page, native);
        await sample.InitializeAsync("header-sidebar-body-footer");
        foreach (var (element, background) in new[]
        {
            (sample.Sidebar.Locator(native ? ".rz-navigation-item-text" : ".admiral-side-menu-item__label").First, sample.Sidebar),
            (sample.Footer, sample.Footer)
        })
        {
            var foregroundColor = await element.GetCssAsync("color");
            var backgroundColor = await background.GetCssAsync("background-color");
            Assert.That(ColorContrast.Ratio(foregroundColor, backgroundColor), Is.GreaterThanOrEqualTo(4.5),
                $"{foregroundColor} text on {backgroundColor}");
        }
        var icon = sample.Sidebar.Locator(native ? ".rz-navigation-item-icon" : ".admiral-side-menu-item__icon").First;
        Assert.That(ColorContrast.Ratio(await icon.GetCssAsync("color"), await sample.Sidebar.GetCssAsync("background-color")),
            Is.GreaterThanOrEqualTo(3), "Sidebar icon contrast");
    }

}
