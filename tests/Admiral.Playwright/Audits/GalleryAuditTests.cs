namespace Admiral.Playwright.Audits;

public sealed class GalleryAuditTests : BrowserTest
{
    internal static string[] Routes() =>
        Directory.GetFiles(
            Path.Combine(Root, "samples/AdmiralConsumerSmoke/Pages"),
            "*.razor")
        .SelectMany(
            file => Regex.Matches(File.ReadAllText(file), "@page\\s+\"([^\"]+)\"").Select(m => m.Groups[1].Value))
        .Distinct()
        .Order()
        .ToArray();

    public static IEnumerable<TestCaseData> StateCases() =>
        from theme in new[]
        {
            "admiral",
            "admiral-dark"
        }

        from route in Routes().Where(r => r is not "/icons" and not "/coverage")
        select new TestCaseData(theme, route).SetName($"State audit: {theme} {route}");

    public static IEnumerable<TestCaseData> LayoutCases()
    {
        var widths = (Environment.GetEnvironmentVariable("AUDIT_WIDTHS") ?? "1440,768").Split(',').Select(int.Parse);
        var filter = Environment.GetEnvironmentVariable("AUDIT_ROUTES")?.Split(',');
        return
            from theme in new[]
            {
                "admiral",
                "admiral-dark"
            }

            from width in widths
            from culture in new[]
            {
                "ru",
                "en"
            }

            from route in Routes().Where(r => filter == null || filter.Contains(r))
            select new TestCaseData(theme, width, culture, route).SetName($"Layout: {theme} {width} {culture} {route}");
    }

    [TestCaseSource(nameof(StateCases)), Category("Audit")]
    public async Task ControlStatesCanBeInspectedAsync(string theme, string route)
    {
        // Arrange: open the requested gallery route and prepare the state inventory.
        var (_, page) = await NewPageAsync(theme, "en");
        await page.GotoAsync(BaseUrl + route, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".admiral-consumer-shell").WaitForAsync();
        await page.WaitForTimeoutAsync(350);
        var probes = new List<object>();

        // Act & assert: inspect Admiral and direct Radzen controls in each available state.
        foreach (var tab in new[]
        {
            "admiral",
            "theme"
        })
        {
            var tabButton = page.Locator($".component-doc-tabs [data-doc-tab=\"{tab}\"]");

            // Act
            if (await tabButton.CountAsync() > 0)
            {
                await tabButton.ClickAsync();
            }
            else if (tab != "admiral")
            {
                continue;
            }

            await page.WaitForTimeoutAsync(150);

            var candidates = page.Locator(
                ".admiral-layout__body button, .admiral-layout__body input, .admiral-layout__body textarea, .admiral-layout__body [tabindex=\"0\"], .admiral-layout__body a[href]");
            var seen = new HashSet<string>();

            // Act & assert
            for (var index = 0; index < await candidates.CountAsync(); index++)
            {
                var target = candidates.Nth(index);

                if (!await target.IsVisibleAsync() || !await target.IsEnabledAsync())
                {
                    continue;
                }

                if (await target.EvaluateAsync<bool>(
                    "e => e.getAttribute('aria-hidden') === 'true' || getComputedStyle(e).pointerEvents === 'none'"))
                {
                    continue;
                }

                var key = await target.EvaluateAsync<string>(
                    "e => `${e.tagName}.${e.className}:${e.getAttribute('role') || e.type || ''}`");

                if (!seen.Add(key) || Regex.IsMatch(key, "side-menu|doc-tabs|component-controls"))
                {
                    continue;
                }

                const string snapshot = "e => { const s = getComputedStyle(e); return { color: s.color, background: s.backgroundColor, border: s.border, radius: s.borderRadius, outline: s.outline, offset: s.outlineOffset, shadow: s.boxShadow, fontWeight: s.fontWeight, focused: document.activeElement === e, focusVisible: e.matches(':focus-visible') }; }";

                var normal = await target.ReadSnapshotAsync<ControlStateSnapshot>(snapshot);

                await target.HoverAsync();

                var hover = await target.ReadSnapshotAsync<ControlStateSnapshot>(snapshot);

                await page.Keyboard.PressAsync("Tab");
                await target.FocusAsync();

                var hoverFocus = await target.ReadSnapshotAsync<ControlStateSnapshot>(snapshot);

                await page.Mouse.MoveAsync(0, 0);

                var focus = await target.ReadSnapshotAsync<ControlStateSnapshot>(snapshot);
                probes.Add(
                    new { tab, identity = key, text = await target.TextContentAsync(), label = await target.GetAttributeAsync("aria-label"), @default = normal, hover, hoverFocus, focus });
            }
        }

        // Diagnostics: save the collected state inventory.
        var file = Path.Combine(Output, "states-" + theme + route.Replace('/', '_') + ".json");

        // Diagnostics
        await File.WriteAllTextAsync(
            file,
            JsonSerializer.Serialize(probes, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(file);
        // State inventory is evidence; it deliberately does not claim design parity.
    }

    [TestCaseSource(nameof(LayoutCases)), Category("Layout")]
    public async Task PageContentFitsTheViewportAsync(string theme, int width, string culture, string route)
    {
        // Arrange: open the route with the requested theme, culture and viewport.
        var (_, page) = await NewPageAsync(theme, culture, width);
        await page.GotoAsync(BaseUrl + route);
        await page.Locator(".admiral-consumer-shell").WaitForAsync();
        await page.WaitForTimeoutAsync(100);
        var tabs = page.Locator(".component-doc-tabs button");
        var count = await tabs.CountAsync();
        var snapshots = new List<JsonElement>();

        // Act & assert: measure each documentation tab at the requested viewport width.
        for (var index = 0; index < Math.Max(1, count); index++)
        {
            // Act
            if (count > 0)
            {
                await tabs.Nth(index).ClickAsync();
            }

            await page.WaitForTimeoutAsync(75);

            // Assert
            var snapshot = await page.EvaluateAsync<JsonElement>(
                """
                () => {
                  const body = document.querySelector('.admiral-layout__body');
                  const rect = body.getBoundingClientRect();
                  const overflow = [
                    ...body.querySelectorAll(
                      'section, .admiral-component-playground, .admiral-component-controls, .component-doc-api, .component-doc-code',
                    ),
                  ]
                    .filter(
                      (e) => e.getBoundingClientRect().width && e.getBoundingClientRect().right > rect.right + 2,
                    )
                    .map((e) => ({
                      tag: e.tagName,
                      class: e.className,
                      right: Math.round(e.getBoundingClientRect().right),
                      limit: Math.round(rect.right),
                    }));
                  return {
                    tab: document.querySelector('.component-doc-tabs [aria-pressed="true"]')?.textContent,
                    documentOverflow:
                      document.documentElement.scrollWidth > document.documentElement.clientWidth + 2,
                    overflow,
                    errorOverlay: getComputedStyle(document.querySelector('#blazor-error-ui')).display !== 'none',
                  };
                }
                """);

            // Act
            snapshots.Add(snapshot);

            // Assert
            Assert.That(snapshot.GetProperty("documentOverflow").GetBoolean(), Is.False, snapshot.ToString());
            Assert.That(snapshot.GetProperty("overflow").GetArrayLength(), Is.Zero, snapshot.ToString());
            Assert.That(snapshot.GetProperty("errorOverlay").GetBoolean(), Is.False, snapshot.ToString());

            // Act
            if (route is "/tables" or "/components/slider" or "/components/typography")
            {
                await page.ScreenshotAsync(
                    new()
                    {
                        Path = Path.Combine(
                            Output,
                            $"layout-{theme}-{width}-{culture}-{route.Replace('/', '_')}-{index}.png")
                    });
            }
        }

        // Diagnostics: save the layout measurements.
        await File.WriteAllTextAsync(
            Path.Combine(
                Output,
                $"layout-{theme}-{width}-{culture}-{route.Replace('/', '_')}.json"),
            JsonSerializer.Serialize(snapshots));
    }
}
