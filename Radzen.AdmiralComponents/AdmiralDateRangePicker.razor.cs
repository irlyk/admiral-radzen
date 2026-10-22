using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace Radzen.Admiral.Components;

public partial class AdmiralDateRangePicker
{
    private IEnumerable<DateTime> selectedDates = [];
    private string inputValue = string.Empty;
    private bool inputInvalid;
    private bool isEditing;

    [Parameter]
    public DateTime? Start { get; set; }

    [Parameter]
    public EventCallback<DateTime?> StartChanged { get; set; }

    [Parameter]
    public DateTime? End { get; set; }

    [Parameter]
    public EventCallback<DateTime?> EndChanged { get; set; }

    [Parameter]
    public DateTime? Min { get; set; }

    [Parameter]
    public DateTime? Max { get; set; }

    [Parameter]
    public string DateFormat { get; set; } = "dd.MM.yyyy";

    [Parameter]
    public string Placeholder { get; set; } = "DD.MM.YYYY — DD.MM.YYYY";

    [Parameter]
    public string Separator { get; set; } = " — ";

    [Parameter]
    public string AriaLabel { get; set; } = "Date range";

    [Parameter]
    public string ToggleAriaLabel { get; set; } = "Toggle date range calendar";

    [Parameter]
    public string PopupAriaLabel { get; set; } = "Date range calendar";

    [Parameter]
    public CultureInfo? Culture { get; set; }

    [Parameter]
    public bool ShowButtonBar { get; set; } = true;

    [Parameter]
    public bool AllowInput { get; set; } = true;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => string.Join(' ', new[]
        {
            "admiral-date-range-picker",
            inputInvalid ? "admiral-date-range-picker--invalid" : null,
            Class
        }
        .Where(value => !string.IsNullOrWhiteSpace(value)));

    private IReadOnlyDictionary<string, object> InputAttributes => new Dictionary<string, object>
    {
        ["aria-hidden"] = "true",
        ["inputmode"] = "none",
        ["readonly"] = "readonly",
        ["tabindex"] = "-1"
    };

    private string FormatRange(DateTime? start, DateTime? end)
    {
        if (!start.HasValue)
        {
            return string.Empty;
        }

        var culture = Culture ?? CultureInfo.CurrentCulture;
        var startText = start.Value.ToString(DateFormat, culture);
        var endText = end?.ToString(DateFormat, culture) ?? string.Empty;
        return $"{startText}{Separator}{endText}";
    }

    protected override void OnParametersSet()
    {
        selectedDates = GetBoundaryDates(Start, End);
        if (!isEditing)
        {
            inputValue = FormatRange(Start, End);
        }
    }

    private void HandleInput(ChangeEventArgs args)
    {
        isEditing = true;
        inputInvalid = false;
        inputValue = args.Value?.ToString() ?? string.Empty;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await CommitInputAsync(force: true);
        }
        else if (args.Key == "Escape")
        {
            isEditing = false;
            inputInvalid = false;
            inputValue = FormatRange(Start, End);
        }
    }

    private Task CommitInputAsync() => CommitInputAsync(force: false);

    private async Task CommitInputAsync(bool force)
    {
        if (!AllowInput || Disabled || (!isEditing && !force))
        {
            return;
        }

        var text = inputValue.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await SetRangeAsync(null, null);
            return;
        }

        var parts = SplitRange(text);
        var culture = Culture ?? CultureInfo.CurrentCulture;
        if (parts.Length is < 1 or > 2 ||
            !DateTime.TryParseExact(parts[0].Trim(), DateFormat, culture,
                DateTimeStyles.AllowWhiteSpaces, out var start) ||
            (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]) &&
             !DateTime.TryParseExact(parts[1].Trim(), DateFormat, culture,
                 DateTimeStyles.AllowWhiteSpaces, out _)))
        {
            MarkInvalid();
            return;
        }

        DateTime? end = null;
        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
        {
            DateTime.TryParseExact(parts[1].Trim(), DateFormat, culture,
                DateTimeStyles.AllowWhiteSpaces, out var parsedEnd);
            end = parsedEnd.Date;
        }

        start = start.Date;
        if ((Min.HasValue && start < Min.Value.Date) ||
            (Max.HasValue && start > Max.Value.Date) ||
            (end.HasValue && (end.Value < start ||
                              (Min.HasValue && end.Value < Min.Value.Date) ||
                              (Max.HasValue && end.Value > Max.Value.Date))))
        {
            MarkInvalid();
            return;
        }

        await SetRangeAsync(start, end);
    }

    private void MarkInvalid()
    {
        inputInvalid = true;
        StateHasChanged();
    }

    private string[] SplitRange(string value)
    {
        if (!string.IsNullOrEmpty(Separator) && value.Contains(Separator, StringComparison.Ordinal))
        {
            return value.Split([Separator], 2, StringSplitOptions.None);
        }

        return Regex.Split(value, @"\s*(?:—|–)\s*|\s+-\s+", RegexOptions.CultureInvariant);
    }

    private async Task HandleDatesChanged(IEnumerable<DateTime>? value)
    {
        var incoming = (value ?? []).Select(date => date.Date).Distinct().ToArray();
        var current = selectedDates.Select(date => date.Date).Distinct().ToArray();
        // Radzen can return either the complete selection or just the newly picked date,
        // depending on whether its internal Multiple state was recreated during a render.
        // Prefer an added date; only treat a removed boundary as the click when nothing was added.
        var changedDate = incoming.Except(current).LastOrDefault();
        if (changedDate == default)
        {
            changedDate = current.Except(incoming).LastOrDefault();
        }

        if (changedDate == default)
        {
            return;
        }

        if (!Start.HasValue || End.HasValue)
        {
            await SetRangeAsync(changedDate, null);
            return;
        }

        var start = Start.Value.Date;
        if (changedDate <= start)
        {
            await SetRangeAsync(changedDate == start ? null : changedDate, null);
            return;
        }

        await SetRangeAsync(start, changedDate);
    }

    private async Task SetRangeAsync(DateTime? start, DateTime? end)
    {
        Start = start;
        End = end;
        isEditing = false;
        inputInvalid = false;
        inputValue = FormatRange(start, end);
        selectedDates = GetBoundaryDates(start, end);
        await StartChanged.InvokeAsync(start);
        await EndChanged.InvokeAsync(end);
    }

    private void RenderDate(DateRenderEventArgs args)
    {
        if (!Start.HasValue)
        {
            return;
        }

        var date = args.Date.Date;
        var start = Start.Value.Date;
        var end = End?.Date;
        var classes = new List<string>();

        if (date == start)
        {
            classes.Add("admiral-date-range-picker__day--start");
        }

        if (end.HasValue && date == end.Value)
        {
            classes.Add("admiral-date-range-picker__day--end");
        }
        else if (end.HasValue && date > start && date < end.Value)
        {
            classes.Add("admiral-date-range-picker__day--inside");
        }

        if (classes.Count > 0)
        {
            args.Attributes["class"] = string.Join(' ', classes);
        }
    }

    private static IEnumerable<DateTime> GetBoundaryDates(DateTime? start, DateTime? end)
    {
        if (!start.HasValue)
        {
            return [];
        }

        return end.HasValue && end.Value.Date != start.Value.Date
            ? [start.Value.Date, end.Value.Date]
            : [start.Value.Date];
    }
}
