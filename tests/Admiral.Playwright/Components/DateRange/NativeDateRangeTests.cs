namespace Admiral.Playwright.Components.DateRange;

[Category("NativeDateRange")]
public sealed class NativeDateRangeTests : BrowserTest
{
    [TestCase("admiral", "en", 1280, 900)]
    [TestCase("admiral-dark", "en", 1280, 900)]
    public async Task NativeAndAdmiralDateRangesAsync(string theme, string culture, int width, int height)
    {
        // Arrange
        var language = culture;
        var viewport = new
        {
            width,
            height
        };
        var (context, page) = await NewPageAsync(theme, culture, width, height);
        var baseUrl = BaseUrl;
        var output = Output;
        await page.GotoAsync(BaseUrl + "/regression/inputs");
        var root = page.Locator("#input-native-range");
        var input = root.Locator("input");
        var popup = page.Locator("#popupinput-native-range");
        await input.WaitForAsync();
        await page.WaitForTimeoutAsync(300);

        // Act: open the native Radzen range calendar.
        await root.Locator(".rz-datepicker-trigger").ClickAsync();

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await page.WaitForTimeoutAsync(300);

        await Assertions.Expect(popup.Locator(".rz-calendar-view")).ToHaveCountAsync(2);

        // Arrange: choose calendar cells and retain a second range for text entry.
        var days = popup.Locator(".rz-calendar-view").First.Locator("td[id]");

        // Assert
        var start = await days.Nth(4).GetAttributeAsync("id");
        var end = await days.Nth(7).GetAttributeAsync("id");
        var typedRange = await days.EvaluateAllAsync<string[]>("cells => cells.slice(10, 14).map(cell => cell.id)");

        string FormatDate(string? id) =>
            string.Join(
                ".",
                (id ?? throw new AssertionException("Calendar cell has no id"))[^10..].Split("-").Reverse()
                .ToArray());

        // Act: select the native range using calendar cells.
        await days.Nth(4).ClickAsync();
        await days.Nth(7).ClickAsync();

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        Assert.That(await input.InputValueAsync(), Is.EqualTo($"{FormatDate(start)} - {FormatDate(end)}"));

        // Act: clear the native range.
        await root.Locator(".rz-dropdown-clear-icon").ClickAsync();

        // Assert
        await page.WaitForFunctionAsync("() => document.querySelector('#input-native-range input').value === ''");

        // Act: enter a range as text and reopen its calendar.
        await input.FillAsync($"{FormatDate(typedRange[0])} - {FormatDate(typedRange[3])}");
        await input.PressAsync("Tab");
        await root.Locator(".rz-datepicker-trigger").ClickAsync();

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await page.WaitForFunctionAsync(
            """
            ({ ids }) => {
              const selected = document.querySelectorAll('#popupinput-native-range td[aria-selected="true"]');
              return selected.length === ids.length && [...selected].every((cell) => ids.includes(cell.id));
            }
            """,
            new { ids = typedRange });
        await page.WaitForTimeoutAsync(300);

        // Diagnostics
        await popup.ScreenshotAsync(new LocatorScreenshotOptions { Path = $"{output}/{theme}.png" });

        // Act: dismiss the native calendar with Escape.
        await input.FocusAsync();
        await input.PressAsync("Escape");

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        // Arrange: load the Admiral range picker.
        await page.GotoAsync(BaseUrl + "/components/date-range-picker");

        // Assert
        await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();

        var demo = page.GetByTestId("date-range-admiral-demo");
        var range = demo.GetByRole(AriaRole.Textbox, new LocatorGetByRoleOptions { Name = "Date range", Exact = true });
        var status = demo.GetByRole(AriaRole.Status);

        // Assert
        var initial = await range.InputValueAsync();

        // Act: submit a reversed range.
        await range.PressAsync("Enter");
        await range.FillAsync("07.10.2026 — 01.10.2026");
        await range.PressAsync("Enter");

        // Assert
        await demo.Locator(".admiral-date-range-picker__input[aria-invalid=\"true\"]").WaitForAsync();

        Assert.That(await status.InnerTextAsync(), Does.Match(new Regex("01.10.2026.*07.10.2026.*Changes: 0")));

        // Act: cancel invalid text and restore the previous value.
        await range.PressAsync("Escape");

        // Assert
        await demo.Locator(".admiral-date-range-picker__input[aria-invalid=\"false\"]").WaitForAsync();

        Assert.That(await range.InputValueAsync(), Is.EqualTo(initial));

        // Act: commit a valid range.
        await range.FillAsync("01.10.2026 — 08.10.2026");
        await range.PressAsync("Enter");

        // Assert
        await status.GetByText(new Regex("08.10.2026.*Changes: 1")).WaitForAsync();

        // Act: clear both range boundaries.
        await range.FillAsync("");
        await range.PressAsync("Tab");

        // Assert
        await status.GetByText(new Regex("Start: —.*End: —.*Changes: 3")).WaitForAsync();

        // Act: choose the range boundaries using the calendar keyboard.
        await demo.Locator(".rz-datepicker-trigger").ClickAsync();

        var calendar = page.Locator(".rz-datepicker-popup-container:visible");
        var cells = calendar.Locator("td[id]:not([aria-disabled=\"true\"])");

        // Act
        await cells.Nth(4).PressAsync("Enter");

        // Assert
        await status.GetByText(new Regex("Changes: 4")).WaitForAsync();

        // Act
        await cells.Nth(7).PressAsync("Enter");

        // Assert
        await status.GetByText(new Regex("Changes: 5")).WaitForAsync();

        // Assert: verify boundary order and the dates strictly inside the range.
        var firstBoundary = calendar.Locator("td.admiral-date-range-picker__day--start");
        var lastBoundary = calendar.Locator("td.admiral-date-range-picker__day--end");

        // Assert
        var firstDate = (await firstBoundary.GetAttributeAsync("id") ?? throw new AssertionException("Missing start date id"))[^10..];
        var lastDate = (await lastBoundary.GetAttributeAsync("id") ?? throw new AssertionException("Missing end date id"))[^10..];

        Assert.That(string.CompareOrdinal(firstDate, lastDate) < 0, Is.True);

        var inside = await calendar.Locator("td.admiral-date-range-picker__day--inside").EvaluateAllAsync<string[]>("days => days.map(day => day.id.slice(-10))");
        var dayCount = (DateTime.Parse(lastDate) - DateTime.Parse(firstDate)).TotalDays;

        Assert.That(inside.Length, Is.EqualTo(dayCount - 1));
        Assert.That(
            inside.All(
                (date) => string.CompareOrdinal((string)date, firstDate) > 0
                    && string.CompareOrdinal((string)date, lastDate) < 0),
            Is.True);
    }
}
