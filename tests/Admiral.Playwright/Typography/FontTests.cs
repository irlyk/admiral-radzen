namespace Admiral.Playwright.Typography;

[Category("Fonts")]
public sealed class FontTests : BrowserTest
{
    [TestCase("admiral", 1280)]
    [TestCase("admiral-dark", 1280)]
    [TestCase("admiral", 390)]
    [TestCase("admiral-dark", 390)]
    public async Task SystemAndConsumerFontsAreUsedAsync(string theme, int width)
    {
        // Arrange: create the browser context and optional external font fixture.
        var (context, page) = await NewPageAsync(theme, culture: "en", width: width, height: 800);
        var baseUrl = BaseUrl;
        const string family = "Admiral Consumer Font Fixture";
        var fixture = FontFixture.FromEnvironment(family);
        var fixtureServer = fixture == null ? null : Own(new StaticHost(fixture.Directory, fonts: fixture));
        var fixtureUrl = fixtureServer?.Url.TrimEnd('/');
        var fixtureFamily = fixture?.ExpectedFamily;
        const string radzenIconFont = "/_content/Radzen.Blazor/fonts/MaterialSymbolsOutlined.woff2";
        const string themeFontPath = "/_content/Radzen.Admiral.Themes/fonts/";
        var requests = new List<string>();
        var failed = new List<string>();
        var fixtureResponses = new List<(string url, int status)>();
        var iconFontStatuses = new List<int>();

        // Assert
        if (fixture != null)
        {
            Assert.That(fixtureFamily, Is.Not.Null.And.Not.Empty, "Set ADMIRAL_FONT_FIXTURE_EXPECTED_FAMILY");
        }

        // Arrange: record application and font requests before navigating.
        page.Request += (_, request) => requests.Add(request.Url);
        page.Response += (_, response) =>
        {
            if (new Uri(response.Url).AbsolutePath.EndsWith(radzenIconFont))
            {
                iconFontStatuses.Add(response!.Status);
            }

            if (fixtureUrl != null && response.Url.StartsWith(fixtureUrl))
            {
                fixtureResponses.Add((response.Url, response!.Status));
            }
        };
        page.RequestFailed += (_, request) =>
        {
            if (new Uri(request.Url).AbsolutePath != "/_blazor/disconnect")
            {
                failed.Add(request.Url + ": " + request.Failure);
            }
        };

        // Act & assert: inspect rendered Latin and Cyrillic glyphs across component pages.
        foreach (var route in new[]
        {
            "/components/button",
            "/tables",
            "/components/drop-down"
        })
        {
            // Act
            var response = await page.GotoAsync(
                baseUrl + route,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Assert
            Assert.That(response!.Status, Is.EqualTo(200), $"{route}: smoke response");
            await page.Locator(".admiral-layout").WaitForAsync();
            await page.Locator("h1").First.WaitForAsync();

            // Act
            if (!string.IsNullOrEmpty(fixtureUrl))
            {
                await page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = $"{fixtureUrl}/font-opt-in.css" });
                await page.EvaluateAsync(
                    """
                    async (name) => {
                      for (const weight of [400, 500, 550]) await document.fonts.load(`${weight} 16px "${name}"`);
                    }
                    """,
                    family);
            }

            // Assert
            var families = await page.ReadSnapshotAsync<FontFamiliesSnapshot>(
                """
                () => ({
                  body: getComputedStyle(document.body).fontFamily,
                  heading: getComputedStyle(document.querySelector('h1')).fontFamily,
                  control: getComputedStyle(document.querySelector('.admiral-button, .rz-button, .rz-dropdown'))
                    .fontFamily,
                })
                """);

            // Assert
            foreach (var (part, value) in new[]
            {
                ("body", families.Body),
                ("heading", families.Heading),
                ("control", families.Control)
            })
            {
                // Assert
                Assert.That(value, Is.Not.Null.And.Not.Empty, $"{route}: {part} has a computed font family");
                if (!string.IsNullOrEmpty(fixtureUrl))
                {
                    Assert.That(value.Contains(family), Is.True, $"{route}: {part} uses consumer font: {value}");
                }
            }

            // Assert
            if (route == "/components/button")
            {
                // Assert
                var iconFaces = await page.EvaluateAsync<string[]>("""
                    async () =>
                      (await document.fonts.load('16px "Admiral Material Symbols"', 'search')).map(
                        (face) => face.status,
                      )
                    """);

                Assert.That(
                    iconFaces,
                    Is.EqualTo(new[] { "loaded" }),
                    "Admiral theme must register a working Radzen icon face");

                var actual = await FontInspector.ReadRenderedFontsAsync(page, "body");
                if (!string.IsNullOrEmpty(fixtureUrl))
                {
                    foreach (var script in new[]
                    {
                        "latin",
                        "cyrillic"
                    })
                    {
                        Assert.That(
                            actual[script].Any((name) => name == fixtureFamily),
                            Is.True,
                            $"{script} glyph uses consumer font {fixtureFamily}: {JsonSerializer.Serialize(actual)}");
                    }
                }

                // Act
                TestContext.Progress.WriteLine(
                    Convert.ToString(
                        $"{theme} {width}px actual glyph fonts {JsonSerializer.Serialize(actual)}"));
            }

            if (route == "/tables")
            {
                var direct = page.Locator(".rz-button").First;

                // Assert
                await direct.WaitForAsync();
                var directFamily = await direct.GetCssAsync("font-family");
                if (!string.IsNullOrEmpty(fixtureUrl))
                {
                    Assert.That(
                        directFamily.Contains(family),
                        Is.True,
                        $"Direct Radzen button uses consumer font: {directFamily}");
                }

                var actual = await FontInspector.ReadRenderedFontsAsync(page, ".rz-button");
                if (!string.IsNullOrEmpty(fixtureUrl))
                {
                    Assert.That(
                        actual["latin"].Any((name) => name == fixtureFamily),
                        Is.True,
                        $"Direct Radzen button Latin glyphs use consumer font: {JsonSerializer.Serialize(actual)}");
                }
            }

            if (route == "/components/drop-down")
            {
                var trigger = page.Locator("[data-testid=\"drop-down-admiral-demo\"] .rz-dropdown").First;

                // Assert
                await trigger.WaitForAsync();

                // Act
                await trigger.ClickAsync();

                var popup = page.Locator(".rz-dropdown-panel:visible").Last;

                // Assert
                await popup.WaitForAsync();
                var popupFamily = await popup.GetCssAsync("font-family");
                if (!string.IsNullOrEmpty(fixtureUrl))
                {
                    Assert.That(popupFamily.Contains(family), Is.True, $"Popup uses consumer font: {popupFamily}");
                }

                var actual = await FontInspector.ReadRenderedFontsAsync(page, ".rz-dropdown-panel:visible");
                if (!string.IsNullOrEmpty(fixtureUrl))
                {
                    Assert.That(
                        actual["latin"].Any((name) => name == fixtureFamily),
                        Is.True,
                        $"Popup Latin glyph uses consumer font: {JsonSerializer.Serialize(actual)}");
                }

                // Act
                await page.Keyboard.PressAsync("Escape");
            }
        }

        // Assert: consumer font files and weights load from the external fixture.
        if (!string.IsNullOrEmpty(fixtureUrl))
        {
            // Assert
            var fontResponses = await page.EvaluateAsync<string[]>(
                """
                () =>
                  performance
                    .getEntriesByType('resource')
                    .filter(
                      (entry) =>
                        entry.name.includes('/font-opt-in.css') || /\.(otf|ttf|woff2?)($|\?)/i.test(entry.name),
                    )
                    .map((entry) => entry.name)
                """);

            Assert.That(
                requests.Any(
                    (url) => url.StartsWith(fixtureUrl) && new Regex("\\.(otf|ttf|woff2?)$").IsMatch(url)),
                Is.True,
                "Browser must request consumer-hosted font bytes over HTTP");
            Assert.That(
                fixtureResponses.Any(
                    (item) => new Regex("\\.(otf|ttf|woff2?)$").IsMatch(item.url) && item.status == 200),
                Is.True,
                $"Consumer font must load over HTTP 200: {JsonSerializer.Serialize(fixtureResponses)}");
            Assert.That(
                fixtureResponses.Where(
                    item => Regex.IsMatch(item.url, @"\.(otf|ttf|woff2?)$", RegexOptions.IgnoreCase) && item.status == 200)
                    .Select(item => item.url)
                    .Distinct().Count() >= 3,
                Is.True,
                "Regular, Medium and SemiBold must each load over HTTP");
            Assert.That(
                fixtureResponses.All((item) => item.status == 200),
                Is.True,
                $"Consumer stylesheet and fonts must load: {JsonSerializer.Serialize(fixtureResponses)}");
            Assert.That(
                fontResponses.Any((url) => url.StartsWith(fixtureUrl)),
                Is.True,
                "Consumer resources appear in performance entries");

            // Assert
            foreach (var weight in new[]
            {
                400,
                500,
                550
            })
            {
                // Assert
                Assert.That(
                    await page.EvaluateAsync<bool>(
                        "([name, value]) => document.fonts.check(`${value} 16px \"${name}\"`)",
                        new object[] { family, weight }),
                    Is.True,
                    $"Consumer font weight {weight} is loaded");
            }
        }

        // Assert: packaged icon fonts load without theme-owned font requests or failures.
        Assert.That(
            iconFontStatuses.Count != 0
                && iconFontStatuses.All((status) => (new[] { 200, 304 }).Contains(status)),
            Is.True,
            $"Radzen icon font must load from Radzen.Blazor: {iconFontStatuses}");
        Assert.That(
            !requests.Any((url) => new Uri(url).AbsolutePath.Contains(themeFontPath)),
            Is.True,
            "Theme must not request font assets from AdmiralRadzen.Themes");
        Assert.That(failed, Is.EqualTo(Array.Empty<object>()), "No failed font or application requests");
    }
}
