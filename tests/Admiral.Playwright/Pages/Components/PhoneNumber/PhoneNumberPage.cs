namespace Admiral.Playwright.Pages.Components.PhoneNumber;

internal sealed class PhoneNumberPage(IPage page) : GalleryPage(page)
{
    public ILocator Phone => Page.Locator(".admiral-phone-input").First;
    public ILocator Country => Phone.Locator(".rz-dropdown");
    public ILocator Input => Phone.Locator("input[inputmode=tel]");
    public ILocator Popup => Page.Locator(".rz-dropdown-panel:visible").First;
    public ILocator Search => Popup.Locator("input").First;
    public ILocator Options => Popup.Locator(".rz-dropdown-item");

    public Task InitializeAsync() => NavigateAsync("/components/phone-number-input");

    public async Task ChooseCountryAsync(string name)
    {
        await Country.ClickAsync();
        await Search.FillAsync(name);
        await Popup.GetByText(name, new() { Exact = true }).ClickAsync();
        await Assertions.Expect(Popup).ToBeHiddenAsync();
    }
}
