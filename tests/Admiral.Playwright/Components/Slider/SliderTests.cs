using Admiral.Playwright.Pages.Components.Slider;

namespace Admiral.Playwright.Components.Slider;

[Category("Slider")]
public sealed class SliderTests : GalleryTest
{
    [TestCase("admiral", TestName = "Slider: Fractional arrows, Home/End, callbacks and ARIA / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Fractional arrows, Home/End, callbacks and ARIA / admiral-dark")]
    public async Task FractionalArrowsHomeEndCallbacksAndARIAAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var h = slider.Handles.First;

        // Act: increase a fractional value using the keyboard.
        await h.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await slider.ExpectValueAsync(h, 5.5);

        // Act: decrease the value by one step.
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await slider.ExpectValueAsync(h, 5.25);

        // Act: jump to the minimum value.
        await Page.Keyboard.PressAsync("Home");

        // Assert
        await slider.ExpectValueAsync(h, 0);

        // Act: jump to the maximum value.
        await Page.Keyboard.PressAsync("End");

        // Assert
        await slider.ExpectValueAsync(h, 10);

        Assert.That(await h.GetAttributeAsync("aria-valuemin"), Is.EqualTo("0"));
        Assert.That(await h.GetAttributeAsync("aria-valuemax"), Is.EqualTo("10"));
        await Page.GetByText(new Regex("Изменений: 4")).WaitForAsync();
    }

    [TestCase("admiral", TestName = "Slider: Range values, independent labels, crossing and focus role exchange / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Range values, independent labels, crossing and focus role exchange / admiral-dark")]
    public async Task RangeValuesIndependentLabelsCrossingAndFocusRoleExchangeAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();

        // Act
        await slider.SetToggleAsync("Range");

        // Assert
        await Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Нижняя граница" }).WaitForAsync();

        var low = slider.Handles.First;
        var high = slider.Handles.Nth(1);

        // Act
        await low.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await slider.ExpectValueAsync(low, 2.5);

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(await high.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        // Act
        await Page.Keyboard.PressAsync("ArrowLeft");

        // Assert
        await slider.ExpectValueAsync(high, 7.5);

        // Act
        await low.FocusAsync();
        await Page.Keyboard.PressAsync("End");

        // Assert
        await slider.ExpectValueAsync(low, 7.5);
        await slider.ExpectValueAsync(high, 10);

        Assert.That(await high.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
        Assert.That(NumericValue.ParseInvariant(await low.GetAttributeAsync("aria-valuemax")), Is.EqualTo(10));

        // Act
        await high.FocusAsync();
        await Page.Keyboard.PressAsync("Home");

        // Assert
        await slider.ExpectValueAsync(low, 0);
        await slider.ExpectValueAsync(high, 7.5);

        Assert.That(await low.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
    }

    [TestCase("admiral", TestName = "Slider: Localized marks, custom template and nearest range endpoint / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Localized marks, custom template and nearest range endpoint / admiral-dark")]
    public async Task LocalizedMarksCustomTemplateAndNearestRangeEndpointAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();

        // Act
        await slider.Slider.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "2,5", Exact = true }).ClickAsync();

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 2.5);

        // Act
        await Page.GetByLabel("Локаль отметок").SelectOptionAsync("en-US");

        // Assert
        await slider.Slider.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "2.5", Exact = true }).WaitForAsync();

        // Act
        await slider.SetToggleAsync("MarkTemplate");

        // Assert
        await slider.Slider.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "2.5 ₽", Exact = true }).WaitForAsync();
        var markLayout = await slider.Slider.Locator(".admiral-slider__mark").ReadSnapshotsAsync<SliderMarkGeometry>("""
            (elements) =>
              elements.map((element) => ({
                height: element.getBoundingClientRect().height,
                lineHeight: Number.parseFloat(getComputedStyle(element).lineHeight),
                whiteSpace: getComputedStyle(element).whiteSpace,
              }))
            """);

        Assert.That(
            markLayout.All(
                (mark) => mark.WhiteSpace == "nowrap" && mark.Height <= mark.LineHeight),
            Is.True,
            "Custom mark labels, including the final currency suffix, must stay on one line");

        Task<bool> EndpointsContainedAsync() => slider.Slider.Locator(".admiral-slider__marks").EvaluateAsync<bool>(
            """
            (element) => {
              const track = element.getBoundingClientRect();
              const first = element.firstElementChild.getBoundingClientRect();
              const last = element.lastElementChild.getBoundingClientRect();
              return Math.abs(first.left - track.left) < 1 && Math.abs(last.right - track.right) < 1;
            }
            """);

        // Assert
        Assert.That(await EndpointsContainedAsync(), Is.True, "Currency endpoints must stay inside the track bounds");

        // Act
        await slider.SetToggleAsync("MarkTemplate", false);

        // Assert
        Assert.That(await EndpointsContainedAsync(), Is.True, "Numeric endpoints must use the same alignment");

        // Act
        await slider.SetToggleAsync("MarkTemplate");
        await slider.SetToggleAsync("Range");
        await slider.Slider.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "0 ₽", Exact = true }).ClickAsync();

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 0);

        // Act
        await slider.Slider.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "10 ₽", Exact = true }).ClickAsync();

        // Assert
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 10);
    }

    [TestCase("admiral", TestName = "Slider: Geometry, hover, focus and nonpersistent range tooltip / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Geometry, hover, focus and nonpersistent range tooltip / admiral-dark")]
    public async Task GeometryHoverFocusAndNonpersistentRangeTooltipAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var h = slider.Handles.First;

        // Assert
        Assert.That(Math.Round((await h.GetBoundsAsync()).Width), Is.EqualTo(20));

        // Act
        await Page.Mouse.MoveAsync((float)0, (float)0);

        // Assert
        Assert.That(await h.EvaluateAsync<string>("e => getComputedStyle(e, '::before').opacity"), Is.EqualTo("0"));

        var normal = await h.GetCssAsync("background-color");

        // Act
        await h.HoverAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "([label, color]) => getComputedStyle(document.querySelector(`[role=slider][aria-label=\"${label}\"]`)).backgroundColor !== color",
            new object?[] { await h.GetAttributeAsync("aria-label"), normal });

        Assert.That(await h.EvaluateAsync<string>("e => getComputedStyle(e, '::before').opacity"), Is.EqualTo("1"));

        // Act
        await h.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        Assert.That(await h.GetCssAsync("outline-width"), Is.EqualTo("2px"));

        // Act
        await Page.GetByLabel("Размер").SelectOptionAsync("M");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-slider').classList.contains('admiral-slider--m')");

        Assert.That(Math.Round((await h.GetBoundsAsync()).Width), Is.EqualTo(16));

        // Act
        await slider.SetToggleAsync("Range");
        await Page.Mouse.MoveAsync((float)0, (float)0);
        await Page.GetByLabel("Range", new PageGetByLabelOptions { Exact = true }).FocusAsync();

        // Assert
        Assert.That(
            await slider.Handles.Nth(1).EvaluateAsync<string>("e => getComputedStyle(e, '::before').opacity"),
            Is.EqualTo("0"));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/{theme}-desktop.png" });
    }

    [TestCase("admiral", TestName = "Slider: Pointer drag snaps to fractional step and updates tooltip / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Pointer drag snaps to fractional step and updates tooltip / admiral-dark")]
    public async Task PointerDragSnapsToFractionalStepAndUpdatesTooltipAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var h = slider.Handles.First;
        var box = await h.GetBoundsAsync();
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();

        // Act
        await Page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(
            (float)(track.X + track.Width * 0.7),
            (float)(box.Y + box.Height / 2),
            new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => Number(document.querySelector('.admiral-slider [role=slider]').getAttribute('aria-valuenow')) !== 5.25");
        var now = NumericValue.ParseInvariant(await h.GetAttributeAsync("aria-valuenow"));

        Assert.That(now * 4, Is.EqualTo(Math.Round(now * 4)));
        Assert.That(now > 6 && now < 8, Is.True);
    }

    [TestCase("admiral", TestName = "Slider: Disabled and skeleton remove all tab stops / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Disabled and skeleton remove all tab stops / admiral-dark")]
    public async Task DisabledAndSkeletonRemoveAllTabStopsAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();

        // Act
        await slider.SetToggleAsync("Disabled");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-slider [role=slider]').tabIndex === -1");

        Assert.That(await slider.Handles.First.GetAttributeAsync("aria-disabled"), Is.EqualTo("true"));
        Assert.That(
            await slider.Slider.GetByRole(AriaRole.Button).EvaluateAllAsync<bool>("es => es.every(e => e.disabled)"),
            Is.True);

        // Act
        await slider.SetToggleAsync("Disabled", false);
        await slider.SetToggleAsync("Skeleton");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-slider').getAttribute('aria-busy') === 'true'");

        Assert.That(
            await slider.Slider.Locator("[role=slider]").First.GetCssAsync("visibility"),
            Is.EqualTo("hidden"));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/{theme}-skeleton.png" });
    }

    [TestCase("admiral", TestName = "Slider: Legacy integer wrapper remains compatible / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Legacy integer wrapper remains compatible / admiral-dark")]
    public async Task LegacyIntegerWrapperRemainsCompatibleAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var legacy = Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Целочисленный ползунок" });

        // Act
        await legacy.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await slider.ExpectValueAsync(legacy, 55);
    }

    [TestCase("admiral", TestName = "Slider: Slow module download preserves the first keyboard action / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Slow module download preserves the first keyboard action / admiral-dark")]
    public async Task SlowModuleDownloadPreservesTheFirstKeyboardActionAsync(string theme)
    {
        // Arrange: keep the page interactive while its slider module is still downloading.
        await StartPageAsync(theme, "ru");
        await Page.RouteAsync("**/admiral-slider.js", async route =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            await route.ContinueAsync();
        });
        var slider = new SliderPage(Page);
        var legacy = Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Целочисленный ползунок" });

        // Act: interact immediately after the page object reports readiness.
        await slider.InitializeAsync();
        await legacy.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert: the first keyboard action reaches the integer wrapper exactly once.
        await slider.ExpectValueAsync(legacy, 55);
    }

    [TestCase("admiral", TestName = "Slider: Narrow viewport, direct Radzen example and API documentation / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Narrow viewport, direct Radzen example and API documentation / admiral-dark")]
    public async Task NarrowViewportDirectRadzenExampleAndAPIDocumentationAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();

        // Act & assert
        foreach (var width in new[]
        {
            1920,
            1280,
            1000,
            800,
            600
        })
        {
            // Act
            await Page.SetViewportSizeAsync(width, 1000);

            // Assert
            var layout = await slider.Slider.ReadSnapshotAsync<SliderLayoutSnapshot>(
                """
                (e) => {
                  const preview = e.closest('.admiral-component-playground__preview');
                  const controls = preview.parentElement.querySelector('.admiral-component-controls');
                  return {
                    slider: e.getBoundingClientRect().toJSON(),
                    preview: preview.getBoundingClientRect().toJSON(),
                    controls: controls.getBoundingClientRect().toJSON(),
                  };
                }
                """);

            Assert.That(
                layout.Slider.X >= layout.Preview.X && layout.Slider.Right <= layout.Preview.Right,
                Is.True,
                "Slider stays within its preview");
            Assert.That(
                layout.Controls.X >= layout.Preview.Right || layout.Controls.Y >= layout.Preview.Bottom,
                Is.True,
                "Controls must not overlap preview");
            if (width == 1280)
            {
                Assert.That(
                    layout.Slider.Y - layout.Preview.Y > 40,
                    Is.True,
                    "Sample is centered instead of pinned to preview top");
            }
        }

        // Act
        await Page.SetViewportSizeAsync(600, 900);

        // Assert
        Assert.That(
            await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"),
            Is.True);

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/{theme}-narrow.png" });

        // Act
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

        var h = Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Прямой Radzen" });

        // Act
        await h.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await slider.ExpectValueAsync(h, 3.75);

        // Act
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"api\"]").ClickAsync();

        // Assert
        await Page.GetByRole(
            AriaRole.Table,
            new PageGetByRoleOptions { Name = "AdmiralNumericSlider", Exact = true }).WaitForAsync();
    }

    [TestCase("admiral", TestName = "Slider: AnyStep continuous values and preserved keyboard increment / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: AnyStep continuous values and preserved keyboard increment / admiral-dark")]
    public async Task AnyStepContinuousValuesAndPreservedKeyboardIncrementAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();

        // Act
        await slider.SetToggleAsync("AnyStep");

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('.admiral-slider').dataset.sliderStep === 'any'");
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();

        // Act
        await Page.Mouse.ClickAsync((float)(track.X + track.Width * 0.6337), (float)(track.Y + track.Height / 2));

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1'");
        var value = NumericValue.ParseInvariant(await slider.Handles.First.GetAttributeAsync("aria-valuenow"));

        Assert.That(Math.Abs(value - 6.337) < 0.04, Is.True);
        Assert.That(Math.Abs(value * 4 - Math.Round(value * 4)) > 0.05, Is.True);

        // Act
        await slider.Handles.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await Page.WaitForFunctionAsync(
            "value => Number(document.querySelector('.admiral-slider [role=slider]').getAttribute('aria-valuenow')) > value + 0.24",
            value);

        Assert.That(
            Math.Abs(
                NumericValue.ParseInvariant(await slider.Handles.First.GetAttributeAsync("aria-valuenow")) - value - 0.25) < 0.000001,
            Is.True);
        Assert.That(await Page.Locator("[data-slider-result]").GetAttributeAsync("data-drag-ends"), Is.EqualTo("1"));
    }

    [TestCase("admiral", TestName = "Slider: Coincident endpoints can separate in both directions / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Coincident endpoints can separate in both directions / admiral-dark")]
    public async Task CoincidentEndpointsCanSeparateInBothDirectionsAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Совместить маркеры", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Нижняя граница" }).WaitForAsync();
        await slider.ExpectValueAsync(slider.Handles.First, 5);
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 5);

        // Act
        await slider.Handles.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 4.75);

        // Act
        await slider.Handles.Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 5.25);

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Совместить маркеры", Exact = true }).ClickAsync();

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 5);
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 5);
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();

        // Act
        await Page.Mouse.ClickAsync((float)(track.X + track.Width * 0.3), (float)(track.Y + track.Height / 2));

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 3);
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 5);
    }

    [TestCase("admiral", TestName = "Slider: Pointer crossing retains capture and follows exchanged role / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Pointer crossing retains capture and follows exchanged role / admiral-dark")]
    public async Task PointerCrossingRetainsCaptureAndFollowsExchangedRoleAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();

        // Act
        await slider.SetToggleAsync("Range");

        // Assert
        await Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Нижняя граница" }).WaitForAsync();
        var box = await slider.Handles.First.GetBoundsAsync();
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();

        // Act
        await Page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(
            (float)(track.X + track.Width * 0.9),
            (float)(box.Y + box.Height / 2),
            new MouseMoveOptions { Steps = 12 });

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 7.75);
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 9);

        Assert.That(
            await slider.Handles.Nth(1).EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await Page.Mouse.MoveAsync(
            (float)(track.X + track.Width * 0.1),
            (float)(box.Y + box.Height / 2),
            new MouseMoveOptions { Steps = 12 });

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 1);
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 7.75);

        Assert.That(
            await slider.Handles.First.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await Page.Mouse.UpAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1'");

        Assert.That(
            await Page.Locator("[data-slider-result]").GetAttributeAsync("data-completion-matched"),
            Is.EqualTo("true"));
    }

    [TestCase("admiral", TestName = "Slider: Pointer completion once, only after final bound value; marks do not complete / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Pointer completion once, only after final bound value; marks do not complete / admiral-dark")]
    public async Task PointerCompletionOnceOnlyAfterFinalBoundValueMarksDoNotCompleteAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var box = await slider.Handles.First.GetBoundsAsync();
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();

        // Act
        await Page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(
            (float)(track.X + track.Width * 0.8),
            (float)(box.Y + box.Height / 2),
            new MouseMoveOptions { Steps = 10 });

        // Assert
        Assert.That(await Page.Locator("[data-slider-result]").GetAttributeAsync("data-drag-ends"), Is.EqualTo("0"));

        // Act
        await Page.Mouse.UpAsync();

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 8);
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1'");

        Assert.That(
            await Page.Locator("[data-slider-result]").GetAttributeAsync("data-completion-matched"),
            Is.EqualTo("true"));
        Assert.That(
            await Page.Locator("[data-slider-result]").GetAttributeAsync("data-last-cancelled"),
            Is.EqualTo("false"));

        // Act
        await slider.Slider.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "0", Exact = true }).ClickAsync();

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 0);

        Assert.That(await Page.Locator("[data-slider-result]").GetAttributeAsync("data-drag-ends"), Is.EqualTo("1"));
    }

    [TestCase("admiral", TestName = "Slider: Pointer cancellation releases gesture and never double-completes / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Pointer cancellation releases gesture and never double-completes / admiral-dark")]
    public async Task PointerCancellationReleasesGestureAndNeverDoubleCompletesAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var box = await slider.Handles.First.GetBoundsAsync();
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();
        var start = new
        {
            pointerId = 77,
            pointerType = "touch",
            clientX = box.X + box.Width / 2,
            clientY = box.Y + box.Height / 2,
            bubbles = true,
            buttons = 1
        };

        // Act
        await slider.Handles.First.DispatchEventAsync("pointerdown", start);
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync(
            "pointermove",
            new { start.pointerId, start.pointerType, clientX = track.X + track.Width * 0.7, start.clientY, start.bubbles, start.buttons });
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync("pointercancel", start);
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync("lostpointercapture", start);
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync("pointerup", start);

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1'");

        Assert.That(
            await Page.Locator("[data-slider-result]").GetAttributeAsync("data-last-cancelled"),
            Is.EqualTo("true"));
        await Assertions.Expect(slider.Slider.Locator(".admiral-slider__handle--dragging")).ToHaveCountAsync(0);

        // Act
        await slider.Handles.First.FocusAsync();
        await Page.Keyboard.PressAsync("Home");

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 0);
    }

    [TestCase("admiral", TestName = "Slider: Browser-emulated touch drag, not a physical-device claim / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Browser-emulated touch drag, not a physical-device claim / admiral-dark")]
    public async Task BrowserEmulatedTouchDragNotAPhysicalDeviceClaimAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var session = await Context.NewCDPSessionAsync(Page);
        try
        {
            var box = await slider.Handles.First.GetBoundsAsync();
            var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();
            var point = new
            {
                id = 1,
                x = box.X + box.Width / 2,
                y = box.Y + box.Height / 2
            };

            // Act
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchStart", touchPoints = new[] { point } });
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchMove", touchPoints = new[] { new { point.id, x = track.X + track.Width * 0.7, point.y } } });
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchEnd", touchPoints = Array.Empty<object>() });

            // Assert
            await Page.WaitForFunctionAsync(
                "() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1'");

            Assert.That(
                await Page.Locator("[data-slider-result]").GetAttributeAsync("data-last-cancelled"),
                Is.EqualTo("false"));
            Assert.That(
                NumericValue.ParseInvariant(await slider.Handles.First.GetAttributeAsync("aria-valuenow")) > 6,
                Is.True);
        }
        finally
        {
            // Cleanup
            await session.DetachAsync();
        }
    }

    [TestCase("admiral", TestName = "Slider: Rapid keyboard actions are not coalesced; custom attributes and Page keys / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Rapid keyboard actions are not coalesced; custom attributes and Page keys / admiral-dark")]
    public async Task RapidKeyboardActionsAreNotCoalescedCustomAttributesAndPageKeysAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var h = slider.Handles.First;

        // Assert
        Assert.That(await h.GetAttributeAsync("data-testid"), Is.EqualTo("single-handle"));
        await Assertions.Expect(h).ToHaveClassAsync(new Regex(@"\bsample-slider-handle\b"));
        Assert.That(await h.GetAttributeAsync("title"), Is.EqualTo("Настраиваемый HTML-атрибут"));

        // Act
        await h.FocusAsync();

        // Act & assert
        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("ArrowRight");
        }

        // Assert
        await slider.ExpectValueAsync(h, 7.25);
        await Page.WaitForFunctionAsync("() => document.querySelector('[data-slider-result]').dataset.changes === '8'");

        // Act
        await Page.Keyboard.PressAsync("PageDown");

        // Assert
        await slider.ExpectValueAsync(h, 4.75);

        // Act
        await Page.Keyboard.PressAsync("PageUp");

        // Assert
        await slider.ExpectValueAsync(h, 7.25);

        Assert.That(await Page.Locator("[data-slider-result]").GetAttributeAsync("data-drag-ends"), Is.EqualTo("0"));

        // Act
        await slider.SetToggleAsync("Range");

        // Assert
        await Page.GetByTestId("lower-handle").WaitForAsync();
        await Page.GetByTestId("upper-handle").WaitForAsync();
    }

    [TestCase("admiral", TestName = "Slider: External reset and disabled-mid-drag discard stale packets / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: External reset and disabled-mid-drag discard stale packets / admiral-dark")]
    public async Task ExternalResetAndDisabledMidDragDiscardStalePacketsAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var box = await slider.Handles.First.GetBoundsAsync();
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();
        var start = new
        {
            pointerId = 99,
            pointerType = "touch",
            clientX = box.X + box.Width / 2,
            clientY = box.Y + box.Height / 2,
            bubbles = true,
            buttons = 1
        };

        // Act
        await slider.Handles.First.DispatchEventAsync("pointerdown", start);
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync(
            "pointermove",
            new { start.pointerId, start.pointerType, clientX = track.X + track.Width * 0.8, start.clientY, start.bubbles, start.buttons });
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Сбросить значение", Exact = true }).ClickAsync();

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 5.25);

        // Act
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync("pointerup", start);

        // Assert
        Assert.That(await Page.Locator("[data-slider-result]").GetAttributeAsync("data-drag-ends"), Is.EqualTo("0"));

        // Act
        await slider.Handles.First.DispatchEventAsync("pointerdown", start);
        await slider.SetToggleAsync("Disabled");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-slider [role=slider]').tabIndex === -1");

        // Act
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync("pointerup", start);

        // Assert
        Assert.That(await Page.Locator("[data-slider-result]").GetAttributeAsync("data-drag-ends"), Is.EqualTo("0"));
    }

    [TestCase("admiral", TestName = "Slider: Changing mode during a gesture ignores incompatible response shapes / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Changing mode during a gesture ignores incompatible response shapes / admiral-dark")]
    public async Task ChangingModeDuringAGestureIgnoresIncompatibleResponseShapesAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var box = await slider.Handles.First.GetBoundsAsync();
        var track = await slider.Slider.Locator(".rz-slider").GetBoundsAsync();
        var start = new
        {
            pointerId = 109,
            pointerType = "touch",
            clientX = box.X + box.Width / 2,
            clientY = box.Y + box.Height / 2,
            bubbles = true,
            buttons = 1
        };

        // Act
        await slider.Handles.First.DispatchEventAsync("pointerdown", start);
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync(
            "pointermove",
            new { start.pointerId, start.pointerType, clientX = track.X + track.Width * 0.8, start.clientY, start.bubbles, start.buttons });
        await slider.SetToggleAsync("Range");

        // Assert
        await Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Нижняя граница" }).WaitForAsync();

        // Act
        await slider.Slider.Locator(".rz-slider").DispatchEventAsync("pointerup", start);
        await Page.WaitForTimeoutAsync(150);

        // Assert
        Assert.That(await Page.Locator("[data-slider-result]").GetAttributeAsync("data-drag-ends"), Is.EqualTo("0"));
        Assert.That(
            await slider.Handles.EvaluateAllAsync<bool>(
                "elements => elements.every(e => Number.isFinite(Number(e.getAttribute('aria-valuenow'))) && !e.style.cssText.includes('NaN'))"),
            Is.True);
        await slider.ExpectValueAsync(slider.Handles.First, 2.25);
        await slider.ExpectValueAsync(slider.Handles.Nth(1), 7.75);
    }

    [TestCase("admiral", TestName = "Slider: Handled arrows stay inside slider while Tab reaches the parent / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Handled arrows stay inside slider while Tab reaches the parent / admiral-dark")]
    public async Task HandledArrowsStayInsideSliderWhileTabReachesTheParentAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        await Page.EvaluateAsync(
            """
            () => {
              window.sliderBubbledKeys = [];
              document.addEventListener('keydown', (event) => window.sliderBubbledKeys.push(event.key));
            }
            """);

        // Act
        await slider.Handles.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await slider.ExpectValueAsync(slider.Handles.First, 5.5);

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        var keys = await Page.EvaluateAsync<string[]>("() => window.sliderBubbledKeys");

        Assert.That(!keys.Contains("ArrowRight"), Is.True);
        Assert.That(keys.Contains("Tab"), Is.True);
    }

    [TestCase("admiral", TestName = "Slider: Off-center thumb click preserves value and completes once / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Off-center thumb click preserves value and completes once / admiral-dark")]
    public async Task OffCenterThumbClickPreservesValueAndCompletesOnceAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var box = await slider.Handles.First.GetBoundsAsync();

        // Act
        await Page.Mouse.ClickAsync((float)(box.X + box.Width / 2 + 5), (float)(box.Y + box.Height / 2));

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1'");
        await slider.ExpectValueAsync(slider.Handles.First, 5.25);
    }

    [TestCase("admiral", TestName = "Slider: Negative minimum anchors fractional steps and off-grid maximum remains reachable / admiral")]
    [TestCase("admiral-dark", TestName = "Slider: Negative minimum anchors fractional steps and off-grid maximum remains reachable / admiral-dark")]
    public async Task NegativeMinimumAnchorsFractionalStepsAndOffGridMaximumRemainsReachableAsync(string theme)
    {
        // Arrange: load the slider demo with the requested theme.
        await StartPageAsync(theme, "ru");
        var slider = new SliderPage(Page);
        await slider.InitializeAsync();
        var h = Page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Смещённый диапазон", Exact = true });

        // Act
        await h.FocusAsync();
        await Page.Keyboard.PressAsync("Home");

        // Assert
        await slider.ExpectValueAsync(h, -2.1);

        // Act
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await slider.ExpectValueAsync(h, -1.9);

        // Act
        await Page.Keyboard.PressAsync("End");

        // Assert
        await slider.ExpectValueAsync(h, 3.05);

        // Act
        await Page.Keyboard.PressAsync("ArrowLeft");

        // Assert
        await slider.ExpectValueAsync(h, 2.9);
    }
}
