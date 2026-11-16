namespace Admiral.Playwright.Pages.Components.FileInput;

internal sealed class FileInputPage(IPage page) : GalleryPage(page)
{
    public string FirstFilePath => Path.Combine(Root, "tools/admiral/fixtures", "file-input-first.svg");
    public string SecondFilePath => Path.Combine(Root, "tools/admiral/fixtures", "file-input-second.svg");
    public ILocator Input => Page.Locator(".admiral-file-input-control input[type=file]");
    public ILocator Result => Page.Locator(".admiral-component-page__result");

    public async Task ChooseFilesAsync(string[] files, bool custom = false, bool keyboard = false)
    {
        var chooserPromise = Page.WaitForFileChooserAsync();
        var trigger = Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = custom ? "Выбрать файлы" : "Добавьте файлы", Exact = true });
        if (keyboard)
        {
            await trigger.PressAsync("Enter");
        }
        else
        {
            await trigger.ClickAsync();
        }

        await (await chooserPromise).SetFilesAsync(files);
    }

    public async Task InitializeAsync()
    {
        await NavigateAsync("/components/file-input");
        await Input.WaitForAsync();
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Multiple", Exact = true }).UncheckAsync();
        await Page.WaitForFunctionAsync("() => !document.querySelector('input[type=file]').multiple");
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Multiple", Exact = true }).CheckAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('input[type=file]').multiple");
    }
}
