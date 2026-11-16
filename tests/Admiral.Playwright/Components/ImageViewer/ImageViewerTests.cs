using Admiral.Playwright.Pages.Components.ImageViewer;

namespace Admiral.Playwright.Components.ImageViewer;

[Category("ImageViewer")]
public sealed class ImageViewerTests : GalleryTest
{
    [TestCase("admiral", TestName = "ImageViewer: wheel and keyboard zoom respect limits without scrolling the document / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: wheel and keyboard zoom respect limits without scrolling the document / admiral-dark")]
    public async Task WheelAndKeyboardZoomRespectLimitsWithoutScrollingTheDocumentAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);

        // Act
        await imageViewer.OpenAsync();

        // Assert
        var scrollY = await Page.EvaluateAsync<double>("() => window.scrollY");

        // Act
        await imageViewer.Preview.HoverAsync();
        await Page.Mouse.WheelAsync((float)0, (float)-200);

        // Assert
        await Page.WaitForFunctionAsync(
            "() => Number(document.querySelector('.admiral-image-viewer__preview').style.transform.match(/scale\\(([^)]+)\\)/)[1]) > 1");

        Assert.That(await Page.EvaluateAsync<double>("() => window.scrollY"), Is.EqualTo(scrollY));

        // Act
        await imageViewer.ZoomToMaximumAsync();

        // Assert
        await Assertions.Expect(
            imageViewer.Overlay.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true })).ToBeDisabledAsync();

        // Act
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await imageViewer.ExpectScaleAsync(2.75);

        // Act
        await Page.Keyboard.PressAsync("ArrowUp");

        // Assert
        await imageViewer.ExpectScaleAsync(3);
    }

    [TestCase("admiral", TestName = "ImageViewer: mouse pan is bounded; navigation and reopening reset transformations / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: mouse pan is bounded; navigation and reopening reset transformations / admiral-dark")]
    public async Task MousePanIsBoundedNavigationAndReopeningResetTransformationsAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);
        await Page.SetViewportSizeAsync(600, 700);

        // Act
        await imageViewer.OpenAsync();
        await imageViewer.ZoomToMaximumAsync();
        await Page.WaitForTimeoutAsync(180);

        // Assert
        var bounds = await imageViewer.Surface.GetBoundsAsync();

        // Act
        await Page.Mouse.MoveAsync((float)(bounds.X + bounds.Width / 2), (float)(bounds.Y + bounds.Height / 2));
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(
            (float)(bounds.X + bounds.Width / 2 + 120),
            (float)(bounds.Y + bounds.Height / 2 + 80),
            new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        // Assert
        var offset = await imageViewer.Surface.EvaluateAsync<double[]>(
            "e => [parseFloat(e.style.getPropertyValue('--admiral-image-pan-x')), parseFloat(e.style.getPropertyValue('--admiral-image-pan-y'))]");

        Assert.That(Math.Abs(offset[0]) > 0 || Math.Abs(offset[1]) > 0, Is.True, "Zoomed image should move");

        // Act
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await imageViewer.ExpectScaleAsync(1);
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-image-viewer__surface').style.getPropertyValue('--admiral-image-pan-x') === '0px'");

        // Act
        await imageViewer.Overlay.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Повернуть вправо", Exact = true }).ClickAsync();
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await imageViewer.Overlay.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть: Городская композиция", Exact = true }).ClickAsync();

        // Assert
        await imageViewer.ExpectScaleAsync(1);

        Assert.That(await imageViewer.Preview.GetAttributeAsync("style"), Does.Match(new Regex("rotate\\(0deg\\)")));

        // Act
        await Page.SetViewportSizeAsync(1440, 1000);
    }

    [TestCase("admiral", TestName = "ImageViewer: synthetic two-pointer pinch changes scale and releases gesture state / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: synthetic two-pointer pinch changes scale and releases gesture state / admiral-dark")]
    public async Task SyntheticTwoPointerPinchChangesScaleAndReleasesGestureStateAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);

        // Act
        await imageViewer.OpenAsync();
        await imageViewer.Preview.DispatchEventAsync(
            "pointerdown",
            new { pointerId = 31, pointerType = "touch", clientX = 400, clientY = 350, button = 0 });
        await imageViewer.Preview.DispatchEventAsync(
            "pointerdown",
            new { pointerId = 32, pointerType = "touch", clientX = 500, clientY = 350, button = 0 });
        await imageViewer.Surface.DispatchEventAsync(
            "pointermove",
            new { pointerId = 32, pointerType = "touch", clientX = 550, clientY = 350 });

        // Assert
        await imageViewer.ExpectScaleAsync(1.5);

        // Act
        await imageViewer.Surface.DispatchEventAsync("pointerup", new { pointerId = 31, pointerType = "touch" });
        await imageViewer.Surface.DispatchEventAsync("pointerup", new { pointerId = 32, pointerType = "touch" });

        // Assert
        Assert.That(await imageViewer.Surface.GetAttributeAsync("data-dragging"), Is.EqualTo("false"));
    }

    [TestCase("admiral", TestName = "ImageViewer: double-click and dialog Enter toggle bounded native scale / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: double-click and dialog Enter toggle bounded native scale / admiral-dark")]
    public async Task DoubleClickAndDialogEnterToggleBoundedNativeScaleAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);

        // Act
        await imageViewer.OpenAsync();

        // Assert
        var native = await imageViewer.Preview.EvaluateAsync<double>(
            "e => Math.max(0.5, Math.min(3, e.naturalWidth / e.offsetWidth))");

        // Act
        await imageViewer.Overlay.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true }).ClickAsync();

        // Assert
        await imageViewer.ExpectScaleAsync(1.25);

        // Act
        await imageViewer.Preview.DblClickAsync();

        // Assert
        await imageViewer.ExpectScaleAsync(native);

        // Act
        await imageViewer.Overlay.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await imageViewer.ExpectScaleAsync(1);
    }

    [TestCase("admiral", TestName = "ImageViewer: error controls stay disabled and a later open recovers / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: error controls stay disabled and a later open recovers / admiral-dark")]
    public async Task ErrorControlsStayDisabledAndALaterOpenRecoversAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);
        await Page.GotoAsync(BaseUrl + "/components/image-viewer");
        await Page.WaitForTimeoutAsync(500);

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть: Недоступное изображение", Exact = true }).ClickAsync();

        // Assert
        await imageViewer.Overlay.GetByRole(AriaRole.Alert).WaitForAsync();

        await Assertions.Expect(
            imageViewer.Overlay.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true })).ToBeDisabledAsync();

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await imageViewer.Overlay.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть: Горный пейзаж", Exact = true }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-image-viewer__preview')?.complete && !document.querySelector('.admiral-image-viewer__message')");

        await Assertions.Expect(
            imageViewer.Overlay.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true })).ToBeEnabledAsync();
    }

    [TestCase("admiral", TestName = "ImageViewer: wheel retains its anchor after rotation and flips / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: wheel retains its anchor after rotation and flips / admiral-dark")]
    public async Task WheelRetainsItsAnchorAfterRotationAndFlipsAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);
        await Page.SetViewportSizeAsync(600, 700);

        // Act & assert
        for (var turn = 0; turn < 4; turn++)
        {
            // Act
            await imageViewer.OpenAsync();

            // Act & assert
            for (var step = 0; step < 4; step++)
            {
                // Act
                await imageViewer.Overlay.GetByRole(
                    AriaRole.Button,
                    new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true }).ClickAsync();

                // Assert
                await imageViewer.ExpectScaleAsync(1 + (step + 1) * 0.25);
            }

            for (var rotation = 0; rotation < turn; rotation++)
            {
                await imageViewer.Overlay.GetByRole(
                    AriaRole.Button,
                    new LocatorGetByRoleOptions { Name = "Повернуть вправо", Exact = true }).ClickAsync();
            }

            // Assert
            await Page.WaitForFunctionAsync(
                "turn => document.querySelector('.admiral-image-viewer__preview').style.transform.includes(`rotate(${turn * 90}deg)`)",
                turn);

            // Act
            if (turn % 2 != 0)
            {
                await imageViewer.Overlay.GetByRole(
                    AriaRole.Button,
                    new LocatorGetByRoleOptions { Name = "Отразить по горизонтали", Exact = true }).ClickAsync();
            }

            await Page.WaitForTimeoutAsync(180);

            // Assert
            var bounds = await imageViewer.Surface.GetBoundsAsync();

            var point = new
            {
                x = bounds.X + bounds.Width / 2 + 15,
                y = bounds.Y + bounds.Height / 2
            };

            // Act
            await Page.Mouse.MoveAsync((float)point.x, (float)point.y);
            await Page.Mouse.WheelAsync((float)0, (float)-100);

            // Assert
            await Page.WaitForFunctionAsync(
                "() => Number(document.querySelector('.admiral-image-viewer__preview').style.transform.match(/scale\\(([^)]+)\\)/)[1]) > 2");
            await Page.WaitForFunctionAsync(
                "() => Math.abs(parseFloat(document.querySelector('.admiral-image-viewer__surface').style.getPropertyValue('--admiral-image-pan-x'))) > 0.1");
            var pan = await imageViewer.Surface.EvaluateAsync<double>(
                "e => parseFloat(e.style.getPropertyValue('--admiral-image-pan-x'))");

            Assert.That(
                Math.Abs((15 - pan) / await imageViewer.GetScaleAsync() - 15.0 / 2) < 0.1,
                Is.True,
                $"{turn * 90}deg: point under cursor must stay fixed");
        }

        // Act
        await Page.SetViewportSizeAsync(1440, 1000);
    }

    [TestCase("admiral", TestName = "ImageViewer: swipe directions and boundaries; short, vertical, cancelled and slow gestures do not navigate / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: swipe directions and boundaries; short, vertical, cancelled and slow gestures do not navigate / admiral-dark")]
    public async Task SwipeDirectionsAndBoundariesShortVerticalCancelledAndSlowGesturesDoNotNavigateAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);

        // Act
        await imageViewer.OpenAsync();

        var counter = imageViewer.Overlay.Locator(".admiral-image-viewer__counter");
        async Task GestureAsync(double dx, double dy, double delay = 0, bool cancelled = false)
        {
            var box = await imageViewer.Surface.GetBoundsAsync();
            var x = box.X + box.Width / 2;
            var y = box.Y + box.Height / 2;
            await imageViewer.Preview.DispatchEventAsync(
                "pointerdown",
                new { pointerId = 71, pointerType = "touch", clientX = x, clientY = y, button = 0 });
            await imageViewer.Surface.DispatchEventAsync(
                "pointermove",
                new { pointerId = 71, pointerType = "touch", clientX = x + dx, clientY = y + dy });
            if (delay != 0)
            {
                await Page.WaitForTimeoutAsync((float)delay);
            }

            await imageViewer.Surface.DispatchEventAsync(
                cancelled ? "pointercancel" : "pointerup",
                new { pointerId = 71, pointerType = "touch", clientX = x + dx, clientY = y + dy });
        }

        // Act
        await GestureAsync(-80, 5);

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-image-viewer__counter').textContent.trim() === '2 / 2'");

        // Act
        await GestureAsync(-80, 0);
        await Page.WaitForTimeoutAsync(150);

        // Assert
        Assert.That(await counter.InnerTextAsync(), Is.EqualTo("2 / 2"));

        // Act
        await GestureAsync(80, 0);

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-image-viewer__counter').textContent.trim() === '1 / 2'");

        // Assert
        foreach (var (dx, dy, delay, cancelled) in new[]
        {
            (-10d, 0d, 0d, false),
            (-30d, 80d, 0d, false),
            (-80d, 0d, 550d, false),
            (-80d, 0d, 0d, true),
            (80d, 0d, 0d, false)
        })
        {
            // Act
            await GestureAsync(dx, dy, delay, cancelled);
            await Page.WaitForTimeoutAsync(150);

            // Assert
            Assert.That(await counter.InnerTextAsync(), Is.EqualTo("1 / 2"));
        }

        // Act
        await imageViewer.Overlay.FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-image-viewer__counter').textContent.trim() === '2 / 2'");
    }

    [TestCase("admiral", TestName = "ImageViewer: browser-emulated touch pinch anchors its centroid and never becomes a swipe / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: browser-emulated touch pinch anchors its centroid and never becomes a swipe / admiral-dark")]
    public async Task BrowserEmulatedTouchPinchAnchorsItsCentroidAndNeverBecomesASwipeAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);
        await Page.SetViewportSizeAsync(600, 700);

        // Act
        await imageViewer.OpenAsync();

        // Act & assert
        for (var step = 0; step < 4; step++)
        {
            // Act
            await imageViewer.Overlay.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true }).ClickAsync();

            // Assert
            await imageViewer.ExpectScaleAsync(1 + (step + 1) * 0.25);
        }

        // Assert
        var box = await imageViewer.Surface.GetBoundsAsync();

        var cx = box.X + box.Width / 2;
        var cy = box.Y + box.Height / 2;

        // Assert
        var session = await Context.NewCDPSessionAsync(Page);
        try
        {
            // Act
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchStart", touchPoints = new[] { new { x = cx - 30, y = cy, id = 1 }, new { x = cx + 30, y = cy, id = 2 } } });
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchMove", touchPoints = new[] { new { x = cx - 30, y = cy, id = 1 }, new { x = cx + 42, y = cy, id = 2 } } });

            // Assert
            await imageViewer.ExpectScaleAsync(2.4);
            await Page.WaitForFunctionAsync(
                "() => Math.abs(parseFloat(document.querySelector('.admiral-image-viewer__surface').style.getPropertyValue('--admiral-image-pan-x')) - 6) < 0.5");

            // Act
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchEnd", touchPoints = new[] { new { x = cx - 30, y = cy, id = 1 } } });
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchMove", touchPoints = new[] { new { x = cx - 110, y = cy, id = 1 } } });
            await session.SendCommandAsync(
                "Input.dispatchTouchEvent",
                new { type = "touchEnd", touchPoints = Array.Empty<object>() });
            await Page.WaitForTimeoutAsync(150);

            // Assert
            Assert.That(
                await imageViewer.Overlay.Locator(".admiral-image-viewer__counter").InnerTextAsync(),
                Is.EqualTo("1 / 2"));
        }
        finally
        {
            // Cleanup
            await session.DetachAsync();
        }

        // Act
        await Page.SetViewportSizeAsync(1440, 1000);
    }

    [TestCase("admiral", TestName = "ImageViewer: toolbar default/hover/press/focus/disabled and close geometry match reference tokens / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: toolbar default/hover/press/focus/disabled and close geometry match reference tokens / admiral-dark")]
    public async Task ToolbarDefaultHoverPressFocusDisabledAndCloseGeometryMatchReferenceTokensAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);

        // Act
        await imageViewer.OpenAsync();

        // Assert
        var rotate = imageViewer.Overlay.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Повернуть вправо", Exact = true });

        Assert.That(await rotate.GetCssAsync("color"), Is.EqualTo("rgb(168, 171, 175)"));

        // Act
        await rotate.HoverAsync();

        // Assert
        Assert.That(await rotate.GetCssAsync("color"), Is.EqualTo("rgb(201, 202, 205)"));
        Assert.That(await rotate.GetCssAsync("background-color"), Is.EqualTo("rgba(0, 0, 0, 0)"));

        // Act
        await Page.Mouse.DownAsync();

        // Assert
        Assert.That(await rotate.GetCssAsync("color"), Is.EqualTo("rgb(227, 229, 231)"));

        // Act
        await Page.Mouse.UpAsync();
        await imageViewer.Overlay.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        var close = imageViewer.Overlay.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Закрыть", Exact = true });

        // Act
        await close.HoverAsync();

        // Assert
        var closeStyle = await close.ReadSnapshotAsync<ImageViewerCloseStyle>("""
            (e) => {
              const s = getComputedStyle(e),
                r = e.getBoundingClientRect(),
                icon = e.querySelector('.admiral-icon').getBoundingClientRect();
              return {
                width: r.width,
                height: r.height,
                radius: s.borderRadius,
                outline: s.outlineStyle,
                highlight: getComputedStyle(e, '::before').backgroundColor,
                icon: icon.width,
              };
            }
            """);

        Assert.That(closeStyle, Is.EqualTo(new ImageViewerCloseStyle(24, 24, "4px", "solid", "rgba(0, 0, 0, 0)", 24)));

        var previous = imageViewer.Overlay.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Предыдущее изображение", Exact = true });

        Assert.That(await previous.GetCssAsync("color"), Is.EqualTo("rgb(130, 134, 139)"));

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await rotate.FocusAsync();
        await rotate.HoverAsync();

        // Assert
        Assert.That(await rotate.GetCssAsync("outline-style"), Is.EqualTo("solid"));
        Assert.That(await rotate.GetCssAsync("border-radius"), Is.EqualTo("4px"));

        var rect = await rotate.GetBoundsAsync();
        var icon = await rotate.Locator(".admiral-icon").GetBoundsAsync();

        Assert.That(icon.Width, Is.EqualTo(24));
        Assert.That(Math.Abs(icon.X + 12 - rect.X - rect.Width / 2) < 0.5, Is.True);

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/image-viewer-toolbar-{theme}.png" });
    }

    [TestCase("admiral", TestName = "ImageViewer: miniature dimensions/icon geometry and 360px toolbar stay inside viewport / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: miniature dimensions/icon geometry and 360px toolbar stay inside viewport / admiral-dark")]
    public async Task MiniatureDimensionsIconGeometryAnd360pxToolbarStayInsideViewportAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);
        await Page.GotoAsync(BaseUrl + "/components/image-viewer");
        await Page.WaitForTimeoutAsync(500);
        var miniature = Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть: Горный пейзаж", Exact = true });

        // Act & assert
        foreach (var (dimension, size, iconSize) in new[]
        {
            ("Xxs", 40, 20),
            ("Xs", 56, 20),
            ("S", 72, 24),
            ("M", 96, 24),
            ("L", 128, 40),
            ("Xl", 192, 40)
        })
        {
            // Act
            await Page.GetByRole(
                AriaRole.Combobox,
                new PageGetByRoleOptions { Name = "Размер", Exact = true }).SelectOptionAsync(dimension);

            // Assert
            await Page.WaitForFunctionAsync(
                "({size}) => document.querySelector('.admiral-image-viewer__miniature').getBoundingClientRect().width === size",
                new { size = size });
            var miniatureIcon = await miniature.Locator(".admiral-icon").GetBoundsAsync();

            Assert.That(miniatureIcon.Width, Is.EqualTo(iconSize));
            Assert.That(miniatureIcon.Height, Is.EqualTo(iconSize));
        }

        // Act
        await Page.SetViewportSizeAsync(360, 740);
        await miniature.ClickAsync();

        // Assert
        await imageViewer.Preview.WaitForAsync();
        var toolbar = await imageViewer.Overlay.Locator(".admiral-image-viewer__toolbar").GetBoundsAsync();

        Assert.That(toolbar.X >= 0 && toolbar.X + toolbar.Width <= 360, Is.True);

        var first = imageViewer.Overlay.Locator(".admiral-image-viewer__toolbar button").First;

        // Assert
        var button = await first.GetBoundsAsync();
        var icon = await first.Locator(".admiral-icon").GetBoundsAsync();

        Assert.That(Math.Abs(button.X + button.Width / 2 - icon.X - icon.Width / 2) < 0.5, Is.True);

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/image-viewer-mobile-{theme}.png" });

        // Act
        await Page.SetViewportSizeAsync(1440, 1000);
    }

    [TestCase("admiral", TestName = "ImageViewer: F fit toggle, native toolbar Enter, focus trap and restore / admiral")]
    [TestCase("admiral-dark", TestName = "ImageViewer: F fit toggle, native toolbar Enter, focus trap and restore / admiral-dark")]
    public async Task FFitToggleNativeToolbarEnterFocusTrapAndRestoreAsync(string theme)
    {
        // Arrange: prepare the viewer page and its image controls.
        await StartPageAsync(theme, "ru");
        var imageViewer = new ImageViewerPage(Page);

        // Act
        await imageViewer.OpenAsync();

        // Assert
        var initial = await imageViewer.GetScaleAsync();

        // Act
        await Page.Keyboard.PressAsync("f");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => Number(document.querySelector('.admiral-image-viewer__preview').style.transform.match(/scale\\(([^)]+)\\)/)[1]) !== 1");

        // Act
        await Page.Keyboard.PressAsync("f");

        // Assert
        await imageViewer.ExpectScaleAsync(initial);

        // Act
        await imageViewer.Overlay.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Увеличить", Exact = true }).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await imageViewer.ExpectScaleAsync(1.25);

        // Act
        await imageViewer.Overlay.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Закрыть", Exact = true }).FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(
            await Page.EvaluateAsync<bool>(
                "() => !!document.activeElement.closest('.admiral-image-viewer__toolbar')"),
            Is.True);

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(
            await imageViewer.Overlay.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Закрыть", Exact = true }).EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/image-viewer-{theme}.png" });

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await imageViewer.Overlay.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(
            await Page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Открыть: Горный пейзаж", Exact = true }).EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));
    }
}
