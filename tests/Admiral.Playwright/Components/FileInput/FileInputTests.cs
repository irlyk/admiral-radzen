using Admiral.Playwright.Pages.Components.FileInput;

namespace Admiral.Playwright.Components.FileInput;

[Category("FileInput")]
public sealed class FileInputTests : GalleryTest
{
    [TestCase("admiral", TestName = "FileInput: External removal preserves native FileList and remaining streams / admiral")]
    [TestCase("admiral-dark", TestName = "FileInput: External removal preserves native FileList and remaining streams / admiral-dark")]
    public async Task ExternalRemovalPreservesNativeFileListAndRemainingStreamsAsync(string theme)
    {
        // Arrange: load the file input demo and locate its fixture files.
        await StartPageAsync(theme, "ru");
        var fileInput = new FileInputPage(Page);
        await fileInput.InitializeAsync();

        // Act
        await fileInput.ChooseFilesAsync(new[] { fileInput.FirstFilePath, fileInput.SecondFilePath });

        // Assert
        await Page.GetByText("file-input-second.svg", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Удалить первый извне", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByText(
            "file-input-first.svg",
            new PageGetByTextOptions { Exact = true }).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        await Page.WaitForFunctionAsync("() => document.querySelector('input[type=file]').files.length === 1");

        Assert.That(
            await fileInput.Input.EvaluateAsync<string[]>("element => Array.from(element.files, file => file.name)"),
            Is.EqualTo(new[] { "file-input-second.svg" }));

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Прочитать потоки", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByText(
            $"Выбрано: 1 · Последнее действие: Прочитано {(await Task.FromResult(new { size = new FileInfo(fileInput.SecondFilePath).Length })).size} байт",
            new PageGetByTextOptions { Exact = true }).WaitForAsync();
    }

    [TestCase("admiral", TestName = "FileInput: File cards use XL type icon and compact M layout / admiral")]
    [TestCase("admiral-dark", TestName = "FileInput: File cards use XL type icon and compact M layout / admiral-dark")]
    public async Task FileCardsUseXLTypeIconAndCompactMLayoutAsync(string theme)
    {
        // Arrange: load the file input demo and locate its fixture files.
        await StartPageAsync(theme, "ru");
        var fileInput = new FileInputPage(Page);
        await fileInput.InitializeAsync();

        // Act
        await fileInput.ChooseFilesAsync(new[] { fileInput.FirstFilePath });

        var card = Page.Locator(".admiral-upload-file");

        // Assert
        await card.WaitForAsync();

        Assert.That(
            await card.EvaluateAsync<bool>(
                "element => element.classList.contains('admiral-upload-file--xl')"),
            Is.True);
        Assert.That(
            await card.Locator(".admiral-upload-file__type").GetAttributeAsync("class"),
            Does.Match(new Regex("admiral-upload-file__type")));

        // Act
        await Page.Locator(".admiral-component-controls select").First.SelectOptionAsync("M");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-upload-file')?.classList.contains('admiral-upload-file--m')");

        await Assertions.Expect(card.Locator(".admiral-upload-file__type")).ToHaveCountAsync(0);
        Assert.That(
            await card.EvaluateAsync<bool>("element => element.getBoundingClientRect().height <= 44"),
            Is.True);
    }

    [TestCase("admiral", TestName = "FileInput: Direct Radzen image preview fits XL and M file rows / admiral")]
    [TestCase("admiral-dark", TestName = "FileInput: Direct Radzen image preview fits XL and M file rows / admiral-dark")]
    public async Task DirectRadzenImagePreviewFitsXLAndMFileRowsAsync(string theme)
    {
        // Arrange: load the file input demo and locate its fixture files.
        await StartPageAsync(theme, "ru");
        var fileInput = new FileInputPage(Page);
        await fileInput.InitializeAsync();

        // Act
        await Page.Locator("[data-doc-tab=theme]").ClickAsync();

        var upload = Page.Locator(".rz-fileupload.admiral-file-input");

        // Act
        await upload.Locator("input[type=file]").SetInputFilesAsync(fileInput.FirstFilePath);

        var row = upload.Locator(".rz-fileupload-row");

        // Assert
        await row.WaitForAsync();
        var xl = await row.GetBoundsAsync();

        Assert.That(xl.Height <= 60, Is.True, $"XL row height: {xl?.Height}");

        var preview = row.Locator(":scope > div:first-child img");

        await Assertions.Expect(preview).ToBeVisibleAsync();

        // Act
        await Page.Locator(".admiral-component-controls select").First.SelectOptionAsync("M");

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('.rz-fileupload.admiral-file-input--m')");
        var m = await row.GetBoundsAsync();

        Assert.That(m.Height <= 44, Is.True, $"M row height: {m?.Height}");
        await Assertions.Expect(preview).Not.ToBeVisibleAsync();

        var columns = await row.ReadSnapshotAsync<FileLabelBounds>(
            """
            (element) => {
              const name = element.querySelector(':scope > div:nth-last-child(3)').getBoundingClientRect();
              const size = element.querySelector(':scope > div:nth-last-child(2)').getBoundingClientRect();
              return { nameRight: name.right, sizeLeft: size.left };
            }
            """);

        Assert.That(
            columns.NameRight <= columns.SizeLeft + 1,
            Is.True,
            $"M filename and size must not overlap: {JsonSerializer.Serialize(columns)}");
    }

    [TestCase("admiral", TestName = "FileInput: Rejected selection clears stale list and reports accessible error / admiral")]
    [TestCase("admiral-dark", TestName = "FileInput: Rejected selection clears stale list and reports accessible error / admiral-dark")]
    public async Task RejectedSelectionClearsStaleListAndReportsAccessibleErrorAsync(string theme)
    {
        // Arrange: load the file input demo and locate its fixture files.
        await StartPageAsync(theme, "ru");
        var fileInput = new FileInputPage(Page);
        await fileInput.InitializeAsync();

        // Act
        await fileInput.ChooseFilesAsync(new[] { fileInput.FirstFilePath });

        // Assert
        await Page.GetByText("file-input-first.svg", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await fileInput.ChooseFilesAsync(
            new[] { Path.Combine(Root, "tools/admiral/fixtures", "file-input-rejected.txt") });

        // Assert
        await Page.GetByText("Недопустимый формат файла", new PageGetByTextOptions { Exact = true }).WaitForAsync();
        await Page.GetByText(
            "file-input-first.svg",
            new PageGetByTextOptions { Exact = true }).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        Assert.That(await fileInput.Input.GetAttributeAsync("aria-invalid"), Is.EqualTo("true"));
        Assert.That(await fileInput.Result.InnerTextAsync(), Does.Match(new Regex("Выбрано: 0")));
    }

    [TestCase("admiral", TestName = "FileInput: Size/count limits, reset and same-file reselection / admiral")]
    [TestCase("admiral-dark", TestName = "FileInput: Size/count limits, reset and same-file reselection / admiral-dark")]
    public async Task SizeCountLimitsResetAndSameFileReselectionAsync(string theme)
    {
        // Arrange: load the file input demo and locate its fixture files.
        await StartPageAsync(theme, "ru");
        var fileInput = new FileInputPage(Page);
        await fileInput.InitializeAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MaxFiles", Exact = true }).FillAsync("1");
        await fileInput.ChooseFilesAsync(new[] { fileInput.FirstFilePath, fileInput.SecondFilePath });

        // Assert
        await Page.GetByText("Слишком много файлов", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MaxFileSize · байты", Exact = true }).FillAsync("100");
        await fileInput.ChooseFilesAsync(new[] { fileInput.FirstFilePath });

        // Assert
        await Page.GetByText(
            "Файл превышает допустимый размер",
            new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Сбросить input", Exact = true }).ClickAsync();

        // Assert
        await Page.GetByRole(AriaRole.Alert).WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        // Act
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MaxFileSize · байты", Exact = true }).FillAsync("10485760");
        await fileInput.ChooseFilesAsync(new[] { fileInput.FirstFilePath });

        // Assert
        await Page.GetByText("file-input-first.svg", new PageGetByTextOptions { Exact = true }).WaitForAsync();
    }

    [TestCase("admiral", TestName = "FileInput: Custom trigger Enter, disabled and skeleton / admiral")]
    [TestCase("admiral-dark", TestName = "FileInput: Custom trigger Enter, disabled and skeleton / admiral-dark")]
    public async Task CustomTriggerEnterDisabledAndSkeletonAsync(string theme)
    {
        // Arrange: load the file input demo and locate its fixture files.
        await StartPageAsync(theme, "ru");
        var fileInput = new FileInputPage(Page);
        await fileInput.InitializeAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { NameRegex = new Regex("Custom trigger|Свой триггер") }).CheckAsync();
        await fileInput.ChooseFilesAsync(new[] { fileInput.FirstFilePath }, true, true);

        // Assert
        await Page.GetByText("file-input-first.svg", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        Assert.That(await fileInput.Input.GetAttributeAsync("tabindex"), Is.EqualTo("-1"));

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Disabled", Exact = true }).CheckAsync();

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('input[type=file]').disabled");

        await Assertions.Expect(
            Page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Выбрать файлы", Exact = true })).Not.ToBeEnabledAsync();
        await Assertions.Expect(
            Page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Удалить файл", Exact = true })).Not.ToBeEnabledAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Disabled", Exact = true }).UncheckAsync();
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Skeleton", Exact = true }).CheckAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-file-input-control__dropzone').getAttribute('aria-busy') === 'true'");

        await Assertions.Expect(fileInput.Input).Not.ToBeEnabledAsync();
    }
}
