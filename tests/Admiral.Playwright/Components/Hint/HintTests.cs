using Admiral.Playwright.Pages.Components.Hint;

namespace Admiral.Playwright.Components.Hint;

[Category("Hint")]
public sealed class HintTests : GalleryTest
{
    [TestCase("admiral", TestName = "Hint: Click: Enter/Space, focus trap, Escape restoration and callbacks / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Click: Enter/Space, focus trap, Escape restoration and callbacks / admiral-dark")]
    public async Task ClickEnterSpaceFocusTrapEscapeRestorationAndCallbacksAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act
        await hint.Opener.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await hint.Popup.WaitForAsync();
        await Page.WaitForFunctionAsync("() => !!document.activeElement?.closest('.admiral-hint__dialog')");

        // Act & assert
        foreach (var key in new[]
        {
            "Shift+Tab",
            "Tab",
            "Tab",
            "Tab"
        })
        {
            // Act
            await Page.Keyboard.PressAsync(key);

            // Assert
            Assert.That(
                await Page.EvaluateAsync<bool>(
                    "() => !!document.activeElement.closest('.admiral-hint__dialog')"),
                Is.True);
        }

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(await hint.Opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
        Assert.That(await hint.Opener.GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));

        // Act
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await hint.Popup.WaitForAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Действие в подсказке", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByText(
            "Открыто: True · Изменений: 3 · Действий: 1",
            new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await hint.CloseAsync();
    }

    [TestCase("admiral", TestName = "Hint: Three widths and theme contrast; close hover/focus/press / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Three widths and theme contrast; close hover/focus/press / admiral-dark")]
    public async Task ThreeWidthsAndThemeContrastCloseHoverFocusPressAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act & assert
        foreach (var (size, width) in new[]
        {
            ("Small", 280),
            ("Medium", 384),
            ("Large", 488)
        })
        {
            // Act
            await Page.GetByLabel("Размер", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync(size);
            await hint.OpenAsync();

            // Assert
            Assert.That(Math.Round((await hint.Popup.GetBoundsAsync()).Width), Is.EqualTo(width));
            await hint.AssertInsideViewportAsync();

            Assert.That(
                await hint.Popup.GetCssAsync("background-color"),
                Is.EqualTo((theme == "admiral") ? "rgb(255, 255, 255)" : "rgb(32, 33, 34)"));

            // Act
            await hint.CloseAsync();
        }

        // Act
        await hint.OpenAsync();

        var button = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Закрыть Hint", Exact = true });

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Действие в подсказке", Exact = true }).FocusAsync();
        await button.HoverAsync();

        // Assert
        Assert.That(await button.GetCssAsync("background-color"), Is.EqualTo("rgba(0, 0, 0, 0)"));
        Assert.That(
            await button.EvaluateAsync<string>("e => getComputedStyle(e, '::before').backgroundColor"),
            Is.Not.EqualTo("rgba(0, 0, 0, 0)"));
        Assert.That(Math.Round((await button.GetBoundsAsync()).Width), Is.EqualTo(20));
        await button.EvaluateAsync("""
            (e) =>
              e.addEventListener(
                'click',
                (event) => {
                  event.preventDefault();
                  event.stopPropagation();
                },
                { once: true },
              )
            """);

        // Act
        await Page.Mouse.DownAsync();

        // Assert
        Assert.That(
            await button.EvaluateAsync<string>("e => getComputedStyle(e, '::before').backgroundColor"),
            Is.EqualTo(
                (theme == "admiral") ? "rgba(0, 0, 0, 0.12)" : "rgba(255, 255, 255, 0.12)"));

        // Act
        await Page.Mouse.UpAsync();
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Действие в подсказке", Exact = true }).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(await button.EvaluateAsync<bool>("e => e.matches(':focus-visible')"), Is.EqualTo(true));
        Assert.That(await button.GetCssAsync("outline-width"), Is.EqualTo("2px"));
        Assert.That(await button.GetCssAsync("border-radius"), Is.EqualTo("4px"));
        Assert.That(
            await button.EvaluateAsync<string>("e => getComputedStyle(e, '::before').backgroundColor"),
            Is.EqualTo("rgba(0, 0, 0, 0)"));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/{theme}-desktop.png" });

        // Act
        await hint.CloseAsync();
    }

    [TestCase("admiral", TestName = "Hint: Four preferred sides and automatic fallback at window corners / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Four preferred sides and automatic fallback at window corners / admiral-dark")]
    public async Task FourPreferredSidesAndAutomaticFallbackAtWindowCornersAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act & assert
        foreach (var side in new[]
        {
            "Top",
            "Right",
            "Bottom",
            "Left",
            "Auto"
        })
        {
            // Act
            await Page.GetByLabel("Положение", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync(side);

            // Assert
            await hint.Opener.EvaluateAsync("""
                (e) => {
                  const anchor = e.parentElement;
                  anchor.style.cssText = 'position:fixed;left:0;top:0;z-index:100';
                  const origin = anchor.getBoundingClientRect();
                  anchor.style.left = `${700 - origin.x}px`;
                  anchor.style.top = `${480 - origin.y}px`;
                }
                """);

            // Act
            await hint.OpenAsync();

            // Assert
            await hint.AssertInsideViewportAsync();

            Assert.That(
                await hint.Popup.GetAttributeAsync("data-placement"),
                Is.EqualTo((side == "Auto") ? "bottom" : side.ToLowerInvariant()));

            // Act
            await hint.CloseAsync();
        }

        foreach (var (x, y) in new[]
        {
            (16, 16),
            (1200, 16),
            (16, 900),
            (1200, 900)
        })
        {
            // Assert
            await hint.Opener.EvaluateAsync("""
                (e, [x, y]) => {
                  const anchor = e.parentElement;
                  anchor.style.cssText = 'position:fixed;left:0;top:0;z-index:100';
                  const origin = anchor.getBoundingClientRect();
                  anchor.style.left = `${x - origin.x}px`;
                  anchor.style.top = `${y - origin.y}px`;
                }
                """, new[] { x, y });
            await hint.Opener.EvaluateAsync("e => e.click()");
            await hint.Popup.WaitForAsync();
            await hint.AssertInsideViewportAsync();

            // Act
            await hint.CloseAsync();
        }
    }

    [TestCase("admiral", TestName = "Hint: Responsive repositioning while open and 360px mobile width / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Responsive repositioning while open and 360px mobile width / admiral-dark")]
    public async Task ResponsiveRepositioningWhileOpenAnd360pxMobileWidthAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act
        await Page.GetByLabel("Положение", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Right");
        await hint.OpenAsync();
        await Page.SetViewportSizeAsync(360, 740);
        await Page.WaitForTimeoutAsync(200);

        // Assert
        await hint.AssertInsideViewportAsync();

        Assert.That(Math.Round((await hint.Popup.GetBoundsAsync()).Width), Is.EqualTo(328));
        Assert.That(
            Enumerable.Contains(
                new[] { "top", "bottom" },
                await hint.Popup.GetAttributeAsync("data-placement")),
            Is.True);

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/{theme}-mobile.png" });

        // Act
        await hint.CloseAsync();
    }

    [TestCase("admiral", TestName = "Hint: Hover: pointer bridge, focus, interactive content and no focus stealing / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Hover: pointer bridge, focus, interactive content and no focus stealing / admiral-dark")]
    public async Task HoverPointerBridgeFocusInteractiveContentAndNoFocusStealingAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act
        await Page.GetByLabel("Триггер", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Hover");
        await hint.Opener.HoverAsync();

        // Assert
        await hint.Popup.WaitForAsync();

        await Assertions.Expect(hint.Popup.Locator(".admiral-hint__close")).ToHaveCountAsync(0);
        Assert.That(
            await Page.EvaluateAsync<bool>("() => !!document.activeElement.closest('.admiral-hint-popup')"),
            Is.EqualTo(false));

        // Act
        await hint.Popup.HoverAsync();
        await Page.WaitForTimeoutAsync(150);

        // Assert
        await Assertions.Expect(hint.Popup).ToBeVisibleAsync();

        // Act
        await Page.Mouse.MoveAsync((float)1400, (float)990);

        // Assert
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act
        await hint.Opener.FocusAsync();

        // Assert
        await hint.Popup.WaitForAsync();

        Assert.That(await hint.Opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(
            await Page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Действие в подсказке", Exact = true }).EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(await hint.Opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    [TestCase("admiral", TestName = "Hint: Hover closes when keyboard focus leaves for a blank page area / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Hover closes when keyboard focus leaves for a blank page area / admiral-dark")]
    public async Task HoverClosesWhenKeyboardFocusLeavesForABlankPageAreaAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act
        await Page.GetByLabel("Триггер", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Hover");
        await Page.Mouse.MoveAsync((float)1400, (float)990);
        await hint.Opener.FocusAsync();

        // Assert
        await hint.Popup.WaitForAsync();

        // Act
        await Page.Mouse.ClickAsync((float)1400, (float)950);

        // Assert
        await Page.WaitForFunctionAsync("() => document.activeElement === document.body");
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await Page.GetByText(
            "Открыто: False · Изменений: 2 · Действий: 0",
            new PageGetByTextOptions { Exact = true }).WaitForAsync();
    }

    [TestCase("admiral", TestName = "Hint: Hover closes on resize; click repositions on scroll / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Hover closes on resize; click repositions on scroll / admiral-dark")]
    public async Task HoverClosesOnResizeClickRepositionsOnScrollAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act
        await Page.GetByLabel("Триггер", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Hover");
        await hint.Opener.HoverAsync();

        // Assert
        await hint.Popup.WaitForAsync();

        // Act
        await Page.SetViewportSizeAsync(1280, 900);

        // Assert
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act
        await Page.Mouse.MoveAsync((float)1200, (float)880);
        await Page.GetByLabel("Триггер", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Click");
        await hint.OpenAsync();

        // Assert
        await Page.EvaluateAsync("() => window.scrollBy(0, 60)");
        await Page.WaitForTimeoutAsync(150);

        await Assertions.Expect(hint.Popup).ToBeVisibleAsync();
        await hint.AssertInsideViewportAsync();

        // Act
        await hint.CloseAsync();
    }

    [TestCase("admiral", TestName = "Hint: Outside-click configuration, focus opt-out and disabled trigger / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Outside-click configuration, focus opt-out and disabled trigger / admiral-dark")]
    public async Task OutsideClickConfigurationFocusOptOutAndDisabledTriggerAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act
        await Page.GetByLabel("Не перемещать фокус", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await Page.GetByLabel("Закрытие снаружи", new PageGetByLabelOptions { Exact = true }).UncheckAsync();
        await hint.OpenAsync();

        // Assert
        Assert.That(await hint.Opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        // Act
        await Page.Mouse.ClickAsync((float)1400, (float)950);

        // Assert
        await Assertions.Expect(hint.Popup).ToBeVisibleAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Действие в подсказке", Exact = true }).FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await Page.WaitForFunctionAsync("() => document.activeElement?.textContent.includes('Открыть Hint')");

        // Act
        await hint.OpenAsync();
        await hint.CloseAsync();
        await Page.GetByLabel("Закрытие снаружи", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await hint.OpenAsync();
        await Page.Mouse.ClickAsync((float)1400, (float)950);

        // Assert
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act
        await Page.GetByLabel("Disabled", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('.admiral-hint-anchor button')?.disabled");

        await Assertions.Expect(hint.Opener).ToBeDisabledAsync();

        // Act
        await hint.Opener.HoverAsync(new LocatorHoverOptions { Force = true });

        // Assert
        await Assertions.Expect(hint.Popup).ToHaveCountAsync(0);
    }

    [TestCase("admiral", TestName = "Hint: Long content scroll, custom trigger and direct Radzen theme example / admiral")]
    [TestCase("admiral-dark", TestName = "Hint: Long content scroll, custom trigger and direct Radzen theme example / admiral-dark")]
    public async Task LongContentScrollCustomTriggerAndDirectRadzenThemeExampleAsync(string theme)
    {
        // Arrange: load the hint demo and its trigger.
        await StartPageAsync(theme, "ru");
        var hint = new HintPage(Page);
        await hint.InitializeAsync();

        // Act
        await Page.GetByLabel("Длинное содержимое", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await hint.OpenAsync();

        // Assert
        var content = hint.Popup.Locator(".admiral-hint__content");

        Assert.That(await content.EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight"), Is.True);
        await content.EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");

        Assert.That(await content.EvaluateAsync<bool>("e => e.scrollTop > 0"), Is.True);
        await hint.AssertInsideViewportAsync();

        // Act
        await hint.CloseAsync();
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Сведения о записи", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByRole(
            AriaRole.Dialog,
            new PageGetByRoleOptions { Name = "Подсказка к иконке", Exact = true }).WaitForAsync();

        var custom = Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Сведения о записи", Exact = true });

        // Assert
        var anchorBox = await custom.Locator("..").GetBoundsAsync();
        var triggerBox = await custom.GetBoundsAsync();
        var popupBox = await hint.Popup.GetBoundsAsync();

        Assert.That(
            Math.Abs(anchorBox.Width - triggerBox.Width) < 1,
            Is.True,
            "Custom anchor must not stretch across its flex parent");
        Assert.That(
            popupBox.X <= triggerBox.X + triggerBox.Width + 8,
            Is.True,
            "Popup must remain near its custom trigger");
        Assert.That(await hint.Popup.Locator(".admiral-hint__content").GetCssAsync("padding-top"), Is.EqualTo("12px"));

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await hint.Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть Radzen Hint", Exact = true }).ClickAsync();

        // Assert
        await Page.Locator(".rz-tooltip.admiral-hint").WaitForAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Закрыть Radzen Hint", Exact = true }).ClickAsync();

        // Assert
        await Page.Locator(".rz-tooltip.admiral-hint").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }
}
