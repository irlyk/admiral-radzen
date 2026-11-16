namespace Admiral.Playwright.Upstream;

[Category("Radzen")]
public sealed class RadzenTests : BrowserTest
{
    private const string TextContrast = """
        (element) => {
          const rgb = (color) => color.match(/[\d.]+/g)?.map(Number) || [0, 0, 0];
          const luminance = (values) =>
            values
              .slice(0, 3)
              .map((v) => {
                v /= 255;
                return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
              })
              .reduce((sum, v, i) => sum + v * [0.2126, 0.7152, 0.0722][i], 0);
          let surface = element;
          while (surface && rgb(getComputedStyle(surface).backgroundColor)[3] === 0)
            surface = surface.parentElement;
          const background = rgb(
            surface ? getComputedStyle(surface).backgroundColor : 'rgb(255, 255, 255)',
          );
          const foreground = rgb(
            getComputedStyle(element)[element instanceof SVGElement ? 'fill' : 'color'],
          );
          const a = luminance(foreground),
            b = luminance(background);
          return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
        }
        """;

    public static IEnumerable<TestCaseData> Cases() =>
        from theme in new[]
        {
            "admiral-regression",
            "admiral-regression-dark"
        }

        from route in new[]
        {
            "/datagrid",
            "/column-chart",
            "/line-chart",
            "/pie-chart",
            "/datepicker",
            "/dropdown",
            "/button",
            "/alert",
            "/alert-styling",
            "/skeleton"
        }

        select new TestCaseData(theme, route);

    [TestCaseSource(nameof(Cases))]
    public async Task ThemeWorksOnDirectRadzenDemosAsync(string theme, string route)
    {
        // Arrange
        var baseUrl = Environment.GetEnvironmentVariable("RADZEN_URL");

        // Assert
        if (string.IsNullOrEmpty(baseUrl))
        {
            Assert.Ignore("Set RADZEN_URL to the isolated upstream demo host");
        }

        var (context, page) = await NewPageAsync(url: baseUrl);
        // External social widgets in the upstream footer are unrelated to component behavior.
        await context.RouteAsync(
            "**/*",
            route => new Uri(route.Request.Url).IsLoopback ? route.ContinueAsync() : route.AbortAsync());
        page.SetDefaultTimeout(10000);
        page.SetDefaultNavigationTimeout(30000);
        var output = Output;
        var result = new RadzenRegressionReport();
        var assetErrors = new List<string>();
        page.Console += (_, message) =>
        {
            if (message.Type == "error" && Regex.IsMatch(message.Text, @"_content/Radzen\.Admiral"))
            {
                assetErrors.Add(message.Text);
            }
        };
        await page.GotoAsync(
            $"{baseUrl}{route}?theme={theme}",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForTimeoutAsync(800);
        var css = await page.ReadSnapshotAsync<StylesheetResponse>(
            """
            async (url) => {
              const response = await fetch(url);
              return { status: response.status, length: (await response.arrayBuffer()).byteLength };
            }
            """,
            $"{baseUrl}/css/{theme}-base.css");

        // Assert
        Assert.That(css.Status, Is.EqualTo(200));
        Assert.That(css.Length > 100000, Is.True, "HTTP 200 must not mask an empty CSS response");
        if (route.EndsWith("-chart"))
        {
            await page.Locator(".rz-chart svg .rz-series-0").First.WaitForAsync();
            await page.WaitForTimeoutAsync(1200);
            result.Series = await page.Locator(".rz-chart svg [class*=\"rz-series-\"]").ReadSnapshotsAsync<ChartSeriesSnapshot>(
                """
                (es) =>
                  es
                    .map((e) => ({
                      token: getComputedStyle(e).getPropertyValue('--rz-series-color').trim(),
                      painted: [...e.querySelectorAll('path,rect,circle')].map((p) => ({
                        fill: getComputedStyle(p).fill,
                        stroke: getComputedStyle(p).stroke,
                      })),
                    }))
                    .filter((s) => s.token)
                """);

            // Assert
            Assert.That(result.Series.Length, Is.Not.Zero, "Series palette must be resolved");
            Assert.That(
                result.Series.Any(
                    (s) => s.Painted.Any(
                        (p) => !(new[] { "none", "rgb(0, 0, 0)" }).Contains(p.Fill)
                            || !(new[] { "none", "rgb(0, 0, 0)" }).Contains(p.Stroke))),
                Is.True,
                "Chart must not fall back to black");

            var tick = page.Locator(".rz-chart .rz-tick-text").First;

            // Assert
            if (await tick.CountAsync() != 0)
            {
                result.TickContrast = await tick.EvaluateAsync<double>(TextContrast);

                Assert.That(
                    result.TickContrast >= 4.5,
                    Is.True,
                    $"Axis label contrast: {result.TickContrast.ToString("F2")}");
            }
        }

        if (route == "/datagrid")
        {
            result.SummaryContrast = await page.Locator(".rz-pager-summary").First.EvaluateAsync<double>(TextContrast);

            // Assert
            Assert.That(
                result.SummaryContrast >= 4.5,
                Is.True,
                $"Pager summary contrast: {result.SummaryContrast.ToString("F2")}");

            var grid = page.Locator(".rz-data-grid-data[role=\"grid\"]").First;

            // Act
            await grid.FocusAsync();
            await page.Keyboard.PressAsync("ArrowDown");

            // Assert
            await page.WaitForFunctionAsync(
                """
                () => {
                  const cell = document.querySelector('.rz-data-grid-data tr.rz-data-row.rz-state-focused > td');
                  return (
                    cell && !['transparent', 'rgba(0, 0, 0, 0)'].includes(getComputedStyle(cell).backgroundColor)
                  );
                }
                """);

            // Act
            await page.Keyboard.PressAsync("ArrowRight");

            // Assert
            await page.WaitForFunctionAsync("""
                () => {
                  const cell = document.querySelector('.rz-data-grid-data td.rz-state-focused');
                  return cell && getComputedStyle(cell).outlineStyle !== 'none';
                }
                """);
        }

        if (route == "/datepicker")
        {
            var trigger = page.Locator(".rz-datepicker-trigger").First;

            // Act
            await trigger.ClickAsync();

            var calendar = page.Locator(".rz-datepicker-popup-container:visible").First;

            // Assert
            await calendar.WaitForAsync();
            await page.WaitForTimeoutAsync(250);
            result.CalendarTitleContrast = await calendar.Locator(".rz-calendar-title").EvaluateAsync<double>(TextContrast);

            Assert.That(
                result.CalendarTitleContrast >= 4.5,
                Is.True,
                $"Calendar title contrast: {result.CalendarTitleContrast.ToString("F2")}");

            var next = calendar.Locator(".rz-calendar-next").First;

            // Act
            await next.HoverAsync();

            // Assert
            var geometry = await next.ReadSnapshotAsync<IconOffsetSnapshot>("""
                (e) => {
                  const box = e.getBoundingClientRect(),
                    icon = e.querySelector('.rzi').getBoundingClientRect();
                  return {
                    dx: (box.left + box.right - icon.left - icon.right) / 2,
                    dy: (box.top + box.bottom - icon.top - icon.bottom) / 2,
                  };
                }
                """);

            Assert.That(
                Math.Abs(geometry.Dx) <= 1 && Math.Abs(geometry.Dy) <= 1,
                Is.True,
                "Calendar navigation icon stays centered on hover");
            result.TocContrast = await page.Locator(".rz-toc-link").First.EvaluateAsync<double>(TextContrast);

            Assert.That(result.TocContrast >= 4.5, Is.True, $"Toc link contrast: {result.TocContrast.ToString("F2")}");

            // Diagnostics
            await page.ScreenshotAsync(
                new PageScreenshotOptions { Path = $"{output}/radzen-{theme}-calendar-hover.png" });

            // Act
            await next.FocusAsync();
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await calendar.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            var input = page.Locator(".rz-datepicker input").First;

            // Assert
            await page.WaitForFunctionAsync("() => document.activeElement?.matches('.rz-datepicker input')");

            // Act
            await page.Keyboard.PressAsync("Alt+ArrowDown");

            // Assert
            await page.Locator(".rz-datepicker-popup-container:visible").First.WaitForAsync();

            // Act
            await input.FocusAsync();
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await page.Locator(".rz-datepicker-popup-container:visible").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        }

        if (route == "/button")
        {
            foreach (var variant in new[]
            {
                "outlined",
                "text"
            })
            {
                var button = page.Locator($".rz-button.rz-variant-{variant}.rz-dark.rz-shade-default").First;
                var demoContrast = await button.EvaluateAsync<double>(TextContrast);
                await button.EvaluateAsync("""
                    (e) => {
                      const probe = e.cloneNode(true);
                      probe.removeAttribute('id');
                      probe.setAttribute('data-admiral-contrast-probe', '');
                      e.closest('.rz-card').append(probe);
                    }
                    """);
                var probe = page.Locator("[data-admiral-contrast-probe]");
                double cardContrast;
                try
                {
                    cardContrast = await probe.EvaluateAsync<double>(TextContrast);
                }
                finally
                {
                    await probe.EvaluateAsync("e => e.remove()");
                }

                result.ButtonContrast[variant] = new ButtonContrast(demoContrast, cardContrast);

                Assert.That(
                    cardContrast >= 4.5,
                    Is.True,
                    $"Dark {variant} button must remain readable on the card surface");
                if (demoContrast < 4.5)
                {
                    result.Warnings.Add(
                        $"Dark {variant} on upstream base-500 demo surface: contrast {demoContrast.ToString("F2")} < 4.5");
                }
            }
        }

        if (route == "/alert-styling")
        {
            // Act
            await page.Locator(".rz-dropdown:visible").Filter(
                new LocatorFilterOptions { Has = page.Locator("input[name=\"AlertStyle\"]") }).ClickAsync();
            await page.Locator(".rz-dropdown-panel:visible").Last.GetByRole(
                AriaRole.Option,
                new LocatorGetByRoleOptions { Name = "Dark", Exact = true }).ClickAsync();

            // Assert
            await page.Locator(".rz-dropdown-panel:visible").WaitForAsync(new() { State = WaitForSelectorState.Hidden });

            // Act & assert
            foreach (var variant in new[]
            {
                "Outlined",
                "Text"
            })
            {
                // Act
                await page.Locator(".rz-dropdown:visible").Filter(
                    new LocatorFilterOptions { Has = page.Locator("input[name=\"Variant\"]") }).ClickAsync();
                await page.Locator(".rz-dropdown-panel:visible").Last.GetByRole(
                    AriaRole.Option,
                    new LocatorGetByRoleOptions { Name = variant, Exact = true }).ClickAsync();

                // Assert
                await page.Locator(".rz-dropdown-panel:visible").WaitForAsync(new() { State = WaitForSelectorState.Hidden });

                var alert = page.Locator($".rz-alert.rz-variant-{variant.ToLowerInvariant()}.rz-dark").First;

                // Assert
                await alert.WaitForAsync();

                Assert.That(
                    await alert.EvaluateAsync<double>(TextContrast) >= 4.5,
                    Is.True,
                    $"Dark {variant} alert must remain readable");
            }
        }

        // Diagnostics
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{output}/radzen-{theme}-{route[1..]}.png" });

        // Assert
        Assert.That(assetErrors, Is.Empty);

        // Diagnostics
        await File.WriteAllTextAsync(
            Path.Combine(output, $"radzen-{theme}-{route[1..]}.json"),
            JsonSerializer.Serialize(result));
    }
}
