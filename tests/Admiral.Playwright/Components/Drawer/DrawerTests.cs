using Admiral.Playwright.Pages.Components.Drawer;

namespace Admiral.Playwright.Components.Drawer;

[Category("Drawer")]
public sealed class DrawerTests : GalleryTest
{
    [TestCase("admiral", TestName = "Drawer: Right/left geometry, width, neutral appearance and close icon states / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Right/left geometry, width, neutral appearance and close icon states / admiral-dark")]
    public async Task RightLeftGeometryWidthNeutralAppearanceAndCloseIconStatesAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act
        await drawer.OpenAsync();

        // Assert
        var bounds = await drawer.Drawer.GetBoundsAsync();
        var backdropBounds = await drawer.Backdrop.GetBoundsAsync();

        Assert.That(
            Math.Round(bounds.X + bounds.Width),
            Is.EqualTo(Math.Round(backdropBounds.X + backdropBounds.Width)));
        Assert.That(Math.Round(bounds.Width), Is.EqualTo(480));

        var primary = await drawer.Drawer.GetCssAsync("background-color");
        var button = drawer.Drawer.Locator(".admiral-drawer__close");

        Assert.That(Math.Round((await button.GetBoundsAsync()).Width), Is.EqualTo(24));
        Assert.That(Math.Round((await button.GetBoundsAsync()).Height), Is.EqualTo(24));

        // Act
        await button.HoverAsync();

        // Assert
        Assert.That(await button.GetCssAsync("background-color"), Is.EqualTo("rgba(0, 0, 0, 0)"));
        Assert.That(
            await button.EvaluateAsync<string>("e => getComputedStyle(e, '::before').backgroundColor"),
            Is.Not.EqualTo("rgba(0, 0, 0, 0)"));
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
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(await button.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
        Assert.That(await button.EvaluateAsync<bool>("""
            (e) => {
              const css = getComputedStyle(e);
              return css.outlineStyle !== 'none' || css.boxShadow !== 'none';
            }
            """), Is.True);

        // Act
        await button.HoverAsync();

        // Assert
        Assert.That(
            await button.EvaluateAsync<string>("e => getComputedStyle(e, '::before').backgroundColor"),
            Is.EqualTo("rgba(0, 0, 0, 0)"));
        Assert.That(await button.GetCssAsync("border-radius"), Is.EqualTo("4px"));

        // Act
        await drawer.CloseAsync();
        await Page.GetByLabel("Сторона", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Left");
        await Page.GetByLabel("Поверхность", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Secondary");
        await Page.GetByLabel("Ширина", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("40rem");
        await drawer.OpenAsync();

        // Assert
        bounds = await drawer.Drawer.GetBoundsAsync();

        Assert.That(Math.Round(bounds.X), Is.EqualTo(0));
        Assert.That(Math.Round(bounds.Width), Is.EqualTo(640));
        Assert.That(await drawer.Drawer.GetCssAsync("background-color"), Is.Not.EqualTo(primary));
        Assert.That(
            await drawer.Drawer.GetCssAsync("background-color"),
            Is.EqualTo((theme == "admiral") ? "rgb(245, 245, 246)" : "rgb(31, 32, 36)"));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/{theme}-left.png" });

        // Act
        await drawer.CloseAsync();
    }

    [TestCase("admiral", TestName = "Drawer: Focus containment, Escape restore, callbacks and panel clicks / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Focus containment, Escape restore, callbacks and panel clicks / admiral-dark")]
    public async Task FocusContainmentEscapeRestoreCallbacksAndPanelClicksAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act
        await drawer.OpenAsync();

        // Assert
        var button = drawer.Drawer.Locator(".admiral-drawer__close");

        Assert.That(await button.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(
            await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()"),
            Is.EqualTo("Отмена"));

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(await button.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        // Act
        await drawer.Drawer.Locator("input").ClickAsync();

        // Assert
        await Assertions.Expect(drawer.Drawer).ToHaveCountAsync(1);

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await drawer.Drawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(await drawer.Opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
        await Page.GetByText(
            "Запросов закрытия: 1 · Пользователь запросил закрытие",
            new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await drawer.OpenAsync();
        await drawer.Drawer.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Сохранить", Exact = true }).ClickAsync();

        // Assert
        await drawer.Drawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await Page.GetByText(
            "Запросов закрытия: 1 · Сохранено: Тестовая запись",
            new PageGetByTextOptions { Exact = true }).WaitForAsync();
    }

    [TestCase("admiral", TestName = "Drawer: Modal backdrop closes; body locked; content scroll leaves actions in place / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Modal backdrop closes; body locked; content scroll leaves actions in place / admiral-dark")]
    public async Task ModalBackdropClosesBodyLockedContentScrollLeavesActionsInPlaceAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act
        await Page.GetByLabel("Длинное содержимое", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await drawer.OpenAsync();

        // Assert
        Assert.That(
            await Page.EvaluateAsync<string>("() => getComputedStyle(document.body).overflowY"),
            Is.EqualTo("hidden"));
        Assert.That(await drawer.Drawer.GetAttributeAsync("aria-modal"), Is.EqualTo("true"));

        var actions = drawer.Drawer.Locator(".admiral-drawer__actions");

        // Assert
        var before = await actions.GetBoundsAsync();
        await drawer.Drawer.Locator(".admiral-drawer__content").EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");

        Assert.That(
            await drawer.Drawer.Locator(".admiral-drawer__content").EvaluateAsync<bool>("e => e.scrollTop > 0"),
            Is.True);
        Assert.That(Math.Round((await actions.GetBoundsAsync()).Y), Is.EqualTo(Math.Round(before.Y)));

        // Act
        await Page.Mouse.ClickAsync((float)10, (float)10);

        // Assert
        await drawer.Drawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(
            await Page.EvaluateAsync<string>("() => getComputedStyle(document.body).overflowY"),
            Is.Not.EqualTo("hidden"));
    }

    [TestCase("admiral", TestName = "Drawer: Without backdrop the page remains usable; Escape outside does not close / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Without backdrop the page remains usable; Escape outside does not close / admiral-dark")]
    public async Task WithoutBackdropThePageRemainsUsableEscapeOutsideDoesNotCloseAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act
        await Page.GetByLabel("Затемнение", new PageGetByLabelOptions { Exact = true }).UncheckAsync();
        await drawer.OpenAsync();

        // Assert
        Assert.That(await drawer.Drawer.GetAttributeAsync("aria-modal"), Is.EqualTo("false"));
        Assert.That(
            await Page.Locator(".admiral-drawer-backdrop").GetCssAsync("background-color"),
            Is.EqualTo("rgba(0, 0, 0, 0)"));
        Assert.That(
            await Page.EvaluateAsync<string>("() => getComputedStyle(document.body).overflowY"),
            Is.Not.EqualTo("hidden"));

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Проверить доступность фоновой страницы", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByText(
            "Нажатий на фоновую страницу: 1",
            new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assertions.Expect(drawer.Drawer).ToHaveCountAsync(1);

        // Act
        await drawer.Drawer.Locator(".admiral-drawer__close").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await drawer.Drawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    [TestCase("admiral", TestName = "Drawer: Non-closable configuration keeps explicit action available / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Non-closable configuration keeps explicit action available / admiral-dark")]
    public async Task NonClosableConfigurationKeepsExplicitActionAvailableAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act & assert
        foreach (var label in new[]
        {
            "Кнопка закрытия",
            "Закрытие по Escape",
            "Закрытие по подложке"
        })
        {
            await Page.GetByLabel(label, new PageGetByLabelOptions { Exact = true }).UncheckAsync();
        }

        // Act
        await drawer.OpenAsync();

        // Assert
        await Assertions.Expect(drawer.Drawer.Locator(".admiral-drawer__close")).ToHaveCountAsync(0);
        Assert.That(
            await drawer.Drawer.Locator("input").EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await Page.Keyboard.PressAsync("Escape");
        await Page.Mouse.ClickAsync((float)10, (float)10);

        // Assert
        await Assertions.Expect(drawer.Drawer).ToHaveCountAsync(1);

        // Act
        await drawer.Drawer.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Отмена", Exact = true }).ClickAsync();

        // Assert
        await drawer.Drawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    [TestCase("admiral", TestName = "Drawer: Mobile forces right edge; narrow layout fits viewport / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Mobile forces right edge; narrow layout fits viewport / admiral-dark")]
    public async Task MobileForcesRightEdgeNarrowLayoutFitsViewportAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act
        await Page.GetByLabel("Сторона", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Left");
        await Page.GetByLabel("Мобильная композиция", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await drawer.OpenAsync();

        // Assert
        Assert.That(
            await drawer.Drawer.EvaluateAsync<bool>("e => e.classList.contains('admiral-drawer--right')"),
            Is.EqualTo(true));
        Assert.That(Math.Round((await drawer.Drawer.GetBoundsAsync()).X), Is.EqualTo(16));

        // Act
        await Page.SetViewportSizeAsync(360, 740);

        // Assert
        var bounds = await drawer.Drawer.GetBoundsAsync();
        var backdropBounds = await drawer.Backdrop.GetBoundsAsync();

        Assert.That(Math.Round(bounds.Width), Is.EqualTo(Math.Round(backdropBounds.Width) - 16));
        Assert.That(Math.Round(bounds.Height), Is.EqualTo(740));
        Assert.That(
            await drawer.Drawer.Locator(".admiral-drawer__actions").GetCssAsync("flex-direction"),
            Is.EqualTo("column-reverse"));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/{theme}-mobile.png" });

        // Act
        await drawer.CloseAsync();
    }

    [TestCase("admiral", TestName = "Drawer: PreventFocusRestore respects opt-out / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: PreventFocusRestore respects opt-out / admiral-dark")]
    public async Task PreventFocusRestoreRespectsOptOutAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act
        await Page.GetByLabel("Не возвращать фокус", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await drawer.OpenAsync();
        await drawer.CloseAsync();

        // Assert
        Assert.That(await drawer.Opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(false));
    }

    [TestCase("admiral", TestName = "Drawer: Exit animation is inert; rapid reopening cancels pending removal / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Exit animation is inert; rapid reopening cancels pending removal / admiral-dark")]
    public async Task ExitAnimationIsInertRapidReopeningCancelsPendingRemovalAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();

        // Act
        await drawer.OpenAsync();
        await drawer.Drawer.Locator(".admiral-drawer__close").ClickAsync();

        // Assert
        await Page.Locator(".admiral-drawer--closing").WaitForAsync();

        Assert.That(await drawer.Drawer.EvaluateAsync<bool>("e => e.inert"), Is.EqualTo(true));
        Assert.That(await drawer.Drawer.GetCssAsync("animation-name"), Does.Match(new Regex("drawer-out-end")));

        // Act
        await drawer.Opener.ClickAsync();
        await Page.WaitForTimeoutAsync(450);

        // Assert
        await Assertions.Expect(drawer.Drawer).ToHaveCountAsync(1);
        Assert.That(await drawer.Drawer.EvaluateAsync<bool>("e => e.inert"), Is.EqualTo(false));

        // Act
        await drawer.CloseAsync();
    }

    [TestCase("admiral", TestName = "Drawer: Reduced motion and independent Radzen example / admiral")]
    [TestCase("admiral-dark", TestName = "Drawer: Reduced motion and independent Radzen example / admiral-dark")]
    public async Task ReducedMotionAndIndependentRadzenExampleAsync(string theme)
    {
        // Arrange: load the drawer demo and its configuration controls.
        await StartPageAsync(theme, "ru");
        var drawer = new DrawerPage(Page);
        await drawer.InitializeAsync();
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });

        // Act
        await drawer.OpenAsync();

        // Assert
        Assert.That(await drawer.Drawer.GetCssAsync("animation-name"), Is.EqualTo("none"));

        // Act
        await drawer.CloseAsync();
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();
        await Page.GetByLabel("Сторона Radzen", new PageGetByLabelOptions { Exact = true }).SelectOptionAsync("Left");
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Открыть Radzen-панель", Exact = true }).ClickAsync();

        // Assert
        await drawer.Drawer.WaitForAsync();

        Assert.That(
            await drawer.Drawer.EvaluateAsync<bool>("e => e.classList.contains('rz-sidebar')"),
            Is.EqualTo(true));
        Assert.That(Math.Round((await drawer.Drawer.GetBoundsAsync()).X), Is.EqualTo(0));

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Закрыть Radzen-панель", Exact = true }).ClickAsync();

        // Assert
        await drawer.Drawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }
}
