using Admiral.Playwright.Pages.Components.ActionBar;

namespace Admiral.Playwright.Components.ActionBar;

[Category("ActionBar")]
public sealed class ActionBarTests : GalleryTest
{
    [TestCase("admiral", TestName = "ActionBar: suffix overflow, exact fit, only-menu width, restore without duplicates / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: suffix overflow, exact fit, only-menu width, restore without duplicates / admiral-dark")]
    public async Task SuffixOverflowExactFitOnlyMenuWidthRestoreWithoutDuplicatesAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Assert: all actions fit before the toolbar is constrained.
        await actionBar.ExpectVisibleActionCountAsync(5);

        await Assertions.Expect(actionBar.OverflowButton).ToHaveCountAsync(0);

        // Act: force trailing actions into the overflow menu.
        await actionBar.SetWidthAsync(200);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(2);

        // Act: inspect the order of the overflowed actions.
        await actionBar.OverflowButton.ClickAsync();

        // Assert
        await actionBar.WaitForMenuFocusAsync();

        Assert.That(await actionBar.GetMenuTextsAsync(), Is.EqualTo(new[] { "Копировать", "Скачать", "Удалить" }));

        // Diagnostics
        await Page.ScreenshotAsync(
            new PageScreenshotOptions { Path = $"{Output}/action-bar-overflow-{theme}.png", FullPage = true });

        // Act: leave enough space for the overflow trigger only.
        await Page.Keyboard.PressAsync("Escape");
        await actionBar.SetWidthAsync(64);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(0);

        await Assertions.Expect(actionBar.OverflowButton).ToHaveCountAsync(1);

        // Act: restore the full toolbar width.
        await actionBar.SetWidthAsync(320);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(5);

        await Assertions.Expect(actionBar.OverflowButton).ToHaveCountAsync(0);
        await Assertions.Expect(actionBar.Toolbar.Locator(".admiral-action-bar__divider")).ToHaveCountAsync(2);
    }

    [TestCase("admiral", TestName = "ActionBar: menu callbacks, disabled exclusion, native Space/Enter and Escape restoration / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: menu callbacks, disabled exclusion, native Space/Enter and Escape restoration / admiral-dark")]
    public async Task MenuCallbacksDisabledExclusionNativeSpaceEnterAndEscapeRestorationAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Act: create an overflow menu with both enabled and disabled actions.
        await actionBar.SetWidthAsync(200);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(2);

        // Act: open the menu from its trigger using the keyboard.
        await actionBar.OverflowButton.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await actionBar.WaitForMenuFocusAsync();
        await Page.GetByRole(
            AriaRole.Menuitem,
            new PageGetByRoleOptions { Name = "Копировать", Exact = true }).WaitForAsync();

        await Assertions.Expect(
            Page.GetByRole(
                AriaRole.Menuitem,
                new PageGetByRoleOptions { Name = "Удалить", Exact = true })).ToBeDisabledAsync();

        // Act: move to the last enabled menu item.
        await Page.Keyboard.PressAsync("End");

        // Assert
        Assert.That(
            await Page.GetByRole(
                AriaRole.Menuitem,
                new PageGetByRoleOptions { Name = "Скачать", Exact = true }).EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act: invoke the first menu action.
        await Page.Keyboard.PressAsync("Home");
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.action-bar-demo-result').textContent.includes('copy')");
        await Page.WaitForFunctionAsync("() => !document.querySelector('[role=\"menu\"]')");

        // Act: reopen the menu with Space.
        await actionBar.OverflowButton.FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await actionBar.WaitForMenuFocusAsync();

        // Act: dismiss the menu and restore focus to its trigger.
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('aria-label') === 'Ещё действия'");

        Assert.That(
            await actionBar.OverflowButton.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act: invoke an action that remains on the toolbar.
        await actionBar.Actions.First.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.action-bar-demo-result').textContent.includes('search')");
    }

    [TestCase("admiral", TestName = "ActionBar: focus survives moving selected action into overflow and back / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: focus survives moving selected action into overflow and back / admiral-dark")]
    public async Task FocusSurvivesMovingSelectedActionIntoOverflowAndBackAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Act: move a focused action into overflow.
        await actionBar.Actions.Nth(3).FocusAsync();
        await actionBar.SetWidthAsync(200);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(2);
        await Page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('aria-label') === 'Ещё действия'");

        // Act: bring the focused action back onto the toolbar.
        await actionBar.SetWidthAsync(400);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(5);
        await Page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('aria-label') === 'Найти'");
    }

    [TestCase("admiral", TestName = "ActionBar: manual overflow and changed Items recompute without losing order / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: manual overflow and changed Items recompute without losing order / admiral-dark")]
    public async Task ManualOverflowAndChangedItemsRecomputeWithoutLosingOrderAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Act: pin an action to the overflow menu.
        await Page.GetByLabel("Постоянные действия в меню", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await actionBar.OverflowButton.ClickAsync();

        // Assert
        await actionBar.WaitForMenuFocusAsync();

        Assert.That(await actionBar.GetMenuTextsAsync(), Is.EqualTo(new[] { "Настройки документа" }));

        // Act: combine pinned and automatically overflowed actions.
        await Page.Keyboard.PressAsync("Escape");
        await actionBar.SetWidthAsync(200);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(2);

        // Act
        await actionBar.OverflowButton.ClickAsync();

        // Assert
        await actionBar.WaitForMenuFocusAsync();

        Assert.That(
            await actionBar.GetMenuTextsAsync(),
            Is.EqualTo(
                new[] { "Копировать", "Скачать", "Удалить", "Настройки документа" }));

        // Act: replace the items while preserving the pinned action.
        await Page.Keyboard.PressAsync("Escape");
        await Page.GetByLabel("Сократить Items до двух", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await actionBar.OverflowButton.ClickAsync();

        // Assert
        await actionBar.WaitForMenuFocusAsync();

        Assert.That(await actionBar.GetMenuTextsAsync(), Is.EqualTo(new[] { "Настройки документа" }));

        // Act: disable automatic overflow and restore the original items.
        await Page.Keyboard.PressAsync("Escape");
        await Page.GetByLabel("AutoOverflow", new PageGetByLabelOptions { Exact = true }).UncheckAsync();
        await Page.GetByLabel("Сократить Items до двух", new PageGetByLabelOptions { Exact = true }).UncheckAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.action-bar-demo-host > .admiral-action-bar > button').length === 5");

        await Assertions.Expect(actionBar.Actions).ToHaveCountAsync(5);
    }

    [TestCase("admiral", TestName = "ActionBar: S/M/L/XL geometry, icon centering and divider widths / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: S/M/L/XL geometry, icon centering and divider widths / admiral-dark")]
    public async Task SMLXLGeometryIconCenteringAndDividerWidthsAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Act
        await Page.GetByLabel("Постоянные действия в меню", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Act & assert: check each supported toolbar dimension.
        foreach (var (size, dimension, icon) in new[]
        {
            ("Small", 32, 20),
            ("Medium", 40, 24),
            ("Large", 48, 24),
            ("ExtraLarge", 56, 24)
        })
        {
            // Act
            await Page.Locator(".admiral-component-controls select").First.SelectOptionAsync(size);
            await Page.WaitForTimeoutAsync(120);

            // Assert
            var geometry = await actionBar.Actions.First.ReadSnapshotAsync<ActionButtonGeometry>("""
                (e) => {
                  const rect = e.getBoundingClientRect(),
                    icon = e.querySelector('.admiral-icon').getBoundingClientRect();
                  return {
                    width: rect.width,
                    height: rect.height,
                    icon: icon.width,
                    dx: icon.x + icon.width / 2 - rect.x - rect.width / 2,
                    dy: icon.y + icon.height / 2 - rect.y - rect.height / 2,
                  };
                }
                """);

            Assert.That(geometry.Width, Is.EqualTo(dimension));
            Assert.That(geometry.Height, Is.EqualTo(dimension));
            Assert.That(geometry.Icon, Is.EqualTo(icon));
            Assert.That(Math.Abs(geometry.Dx) < 1 && Math.Abs(geometry.Dy) < 1, Is.True);
            Assert.That(
                await actionBar.Toolbar.Locator(".admiral-action-bar__divider").First.EvaluateAsync<double>("e => e.getBoundingClientRect().width"),
                Is.EqualTo((size == "Small") ? 16 : 20));

            // Act
            await actionBar.OverflowButton.ClickAsync();

            // Assert
            await actionBar.WaitForMenuFocusAsync();
            var menuGeometry = await Page.GetByRole(AriaRole.Menuitem).First.ReadSnapshotAsync<MenuItemGeometry>(
                "e => ({ height: e.getBoundingClientRect().height, icon: e.querySelector('.admiral-icon').getBoundingClientRect().width })");

            Assert.That(menuGeometry.Height, Is.EqualTo(Math.Min(dimension, 48)));
            Assert.That(menuGeometry.Icon, Is.EqualTo(icon));

            // Act
            await Page.Keyboard.PressAsync("Escape");
        }
    }

    [TestCase("admiral", TestName = "ActionBar: default/hover/focus/hover+focus/active/disabled reuse IconButton states / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: default/hover/focus/hover+focus/active/disabled reuse IconButton states / admiral-dark")]
    public async Task DefaultHoverFocusHoverFocusActiveDisabledReuseIconButtonStatesAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();
        var button = actionBar.Actions.First;
        Task<ActionButtonStyle> ReadStyleAsync() =>
            button.ReadSnapshotAsync<ActionButtonStyle>(
                "e => { const s = getComputedStyle(e); return { color: s.color, background: s.backgroundColor, outline: s.outlineStyle, radius: s.borderRadius }; }");

        // Act: establish the default visual state.
        await Page.Mouse.MoveAsync((float)0, (float)0);

        // Assert
        var normal = await ReadStyleAsync();

        // Act: hover the action.
        await button.HoverAsync();

        // Assert
        var hover = await ReadStyleAsync();

        Assert.That(hover.Background, Is.Not.EqualTo(normal.Background));

        // Act: apply keyboard focus while the pointer remains over the action.
        await Page.Keyboard.PressAsync("Tab");
        await button.FocusAsync();

        // Assert
        Assert.That((await ReadStyleAsync()).Outline, Is.EqualTo("solid"));
        Assert.That((await ReadStyleAsync()).Background, Is.EqualTo(hover.Background));

        // Act: inspect the pressed state.
        await Page.Mouse.DownAsync();

        // Assert
        var active = await ReadStyleAsync();

        // Act
        await Page.Mouse.UpAsync();

        // Assert
        Assert.That(active.Background, Is.Not.EqualTo(hover.Background));

        // Act: disable the action without losing the shared button styling.
        await Page.GetByLabel("Disabled", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-action-bar--adaptive > button')?.disabled");

        await Assertions.Expect(button).ToBeDisabledAsync();
        Assert.That((await ReadStyleAsync()).Color, Is.Not.EqualTo(normal.Color));

        // Diagnostics
        await Page.ScreenshotAsync(
            new PageScreenshotOptions { Path = $"{Output}/action-bar-{theme}.png", FullPage = true });
    }

    [TestCase("admiral", TestName = "ActionBar: resize closes stale popup and real viewport resize recomputes width / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: resize closes stale popup and real viewport resize recomputes width / admiral-dark")]
    public async Task ResizeClosesStalePopupAndRealViewportResizeRecomputesWidthAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Act: open overflow in a constrained toolbar.
        await actionBar.SetWidthAsync(200);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(2);

        // Act
        await actionBar.OverflowButton.ClickAsync();
        await actionBar.SetWidthAsync(64);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(0);
        await Page.WaitForFunctionAsync("() => !document.querySelector('[role=\"menu\"]')");

        // Act: enlarge the toolbar while its overflow menu is open.
        await actionBar.SetWidthAsync(640);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(5);

        // Act: resize the browser to a narrow viewport.
        await Page.SetViewportSizeAsync(360, 900);

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.admiral-action-bar--adaptive > button').length < 5");
        var bounds = await actionBar.Toolbar.GetBoundsAsync();

        // Assert
        foreach (var button in await actionBar.Toolbar.Locator(".admiral-action-bar__item").AllAsync())
        {
            // Assert
            var child = await button.GetBoundsAsync();

            Assert.That(child.X + child.Width <= bounds.X + bounds.Width + 1, Is.True);
        }

        // Diagnostics
        await Page.ScreenshotAsync(
            new PageScreenshotOptions { Path = $"{Output}/action-bar-mobile-{theme}.png", FullPage = true });

        // Act
        await Page.SetViewportSizeAsync(1440, 1000);

        // Assert
        await actionBar.ExpectVisibleActionCountAsync(5);
    }

    [TestCase("admiral", TestName = "ActionBar: GroupActionsPane search Escape, columns and settings remain functional / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: GroupActionsPane search Escape, columns and settings remain functional / admiral-dark")]
    public async Task GroupActionsPaneSearchEscapeColumnsAndSettingsRemainFunctionalAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Act: open and dismiss inline search.
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть поиск", Exact = true }).ClickAsync();

        var search = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Поиск", Exact = true });

        // Act
        await search.FillAsync("документ");
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await search.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act: open the column chooser.
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Выбрать колонки", Exact = true }).ClickAsync();

        var popup = Page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Name = "Колонки", Exact = true });

        // Assert
        await popup.WaitForAsync();

        // Act: toggle a column without closing the chooser.
        await popup.Locator("label").Filter(new LocatorFilterOptions { HasText = "Комментарий" }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => [...document.querySelectorAll('[role=\"dialog\"] label')].find(e => e.textContent.includes('Комментарий'))?.querySelector('[role=\"checkbox\"]')?.getAttribute('aria-checked') === 'true'");

        // Act: inspect the settings action.
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Настройки", Exact = true }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.action-bar-demo-result').textContent.includes('Открыть настройки')");
    }

    [TestCase("admiral", TestName = "ActionBar: table GroupActionsPane actions keep focus ring and hover contrast / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: table GroupActionsPane actions keep focus ring and hover contrast / admiral-dark")]
    public async Task TableGroupActionsPaneActionsKeepFocusRingAndHoverContrastAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();
        await Page.GotoAsync(BaseUrl + "/tables");
        var panes = Page.Locator(".admiral-group-actions-pane");
        await panes.First.WaitForAsync();
        await Page.WaitForTimeoutAsync(250);

        // Act & assert: verify the focus ring and contrast for each table action pane.
        foreach (var pane in await panes.AllAsync())
        {
            var action = pane.Locator(".admiral-group-actions-pane__actions > button").First;

            // Act
            if (!(await action.CountAsync() != 0) || !await action.IsVisibleAsync())
            {
                continue;
            }

            await action.FocusAsync();

            // Assert
            var focus = await action.ReadSnapshotAsync<GroupActionGeometry>(
                """
                (element) => ({
                  visible: element.matches(':focus-visible'),
                  offset: parseFloat(getComputedStyle(element).outlineOffset),
                  gap:
                    element.getBoundingClientRect().left -
                    element.closest('.admiral-group-actions-pane').getBoundingClientRect().left,
                  height: element.getBoundingClientRect().height,
                  paneHeight: element.closest('.admiral-group-actions-pane').getBoundingClientRect().height,
                })
                """);

            Assert.That(
                focus.Visible && focus.Offset == 2 && Math.Abs(focus.Gap) < 1
                    && focus.Height == focus.PaneHeight,
                Is.True,
                $"text action differs from the reference pane: {JsonSerializer.Serialize(focus)}");

            var clipping = await action.ReadSnapshotAsync<FocusClipping>("""
                (element) => {
                  const pane = element.closest('.admiral-group-actions-pane');
                  const viewport = pane.closest('.admiral-table-gallery__scroll');
                  const paneRect = pane.getBoundingClientRect();
                  const viewportRect = viewport?.getBoundingClientRect();
                  return {
                    zIndex: Number(getComputedStyle(pane).zIndex),
                    room: viewportRect && {
                      left: paneRect.left - viewportRect.left,
                      top: paneRect.top - viewportRect.top,
                      right: viewportRect.right - paneRect.right,
                    },
                  };
                }
                """);

            Assert.That(
                clipping.ZIndex >= 3,
                Is.True,
                $"focused pane is behind the grid header: {JsonSerializer.Serialize(clipping)}");
            if (clipping.Room is not null)
            {
                Assert.That(
                    new[] { clipping.Room.Left, clipping.Room.Top, clipping.Room.Right }.All((space) => space >= 4),
                    Is.True,
                    $"scroll viewport clips the 4px focus ring: {JsonSerializer.Serialize(clipping)}");
            }

            var tool = pane.Locator(".admiral-group-actions-pane__tools > button").First;
            if (await tool.CountAsync() != 0)
            {
                // Act
                await Page.Keyboard.PressAsync("Tab");
                await tool.FocusAsync();

                // Assert
                Assert.That(
                    await tool.EvaluateAsync<bool>("element => element.matches(':focus-visible')"),
                    Is.EqualTo(true));
                Assert.That(
                    await tool.EvaluateAsync<double>(
                        "element => parseFloat(getComputedStyle(element).outlineOffset)"),
                    Is.EqualTo(focus.Offset),
                    "text and icon actions use the same focus offset");
            }

            var baseColor = await action.GetCssAsync("color");

            // Act
            await action.HoverAsync();

            // Assert
            Assert.That(await action.GetCssAsync("color"), Is.Not.EqualTo(baseColor));
        }
    }

    [TestCase("admiral", TestName = "ActionBar: API and direct theme tabs are runnable independently / admiral")]
    [TestCase("admiral-dark", TestName = "ActionBar: API and direct theme tabs are runnable independently / admiral-dark")]
    public async Task APIAndDirectThemeTabsAreRunnableIndependentlyAsync(string theme)
    {
        // Arrange: load the adaptive toolbar demo.
        await StartPageAsync(theme, "ru");
        var actionBar = new ActionBarPage(Page);
        await actionBar.InitializeAsync();

        // Act: open the API documentation.
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"api\"]").ClickAsync();

        // Assert
        await Page.GetByRole(
            AriaRole.Table,
            new PageGetByRoleOptions { Name = "AdmiralActionBar", Exact = true }).WaitForAsync();

        // Act: run the independent direct Radzen example.
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();
        await Page.GetByRole(
            AriaRole.Toolbar,
            new PageGetByRoleOptions { Name = "Прямой Radzen", Exact = true }).GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Найти", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByText("Radzen: найти", new PageGetByTextOptions { Exact = true }).WaitForAsync();
    }
}
