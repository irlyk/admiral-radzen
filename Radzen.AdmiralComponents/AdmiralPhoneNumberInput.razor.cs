using Microsoft.AspNetCore.Components;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralPhoneNumberInput
{

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public System.Linq.Expressions.Expression<Func<string?>>? ValueExpression { get; set; }

    [Parameter]
    public string? Country { get; set; } = "RUS";

    [Parameter]
    public EventCallback<string?> CountryChanged { get; set; }

    [Parameter]
    public IEnumerable<AdmiralPhoneCountry>? Countries { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Placeholder { get; set; } = "+7 999 999-99-99";

    [Parameter]
    public AdmiralInputSize Size { get; set; } = AdmiralInputSize.Medium;

    [Parameter]
    public AdmiralInputStatus Status { get; set; }

    [Parameter]
    public AdmiralPhoneValueMode ValueMode { get; set; }

    [Parameter]
    public bool AllowCountrySearch { get; set; } = true;

    [Parameter]
    public bool DisplayClearIcon { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public bool Skeleton { get; set; }

    [Parameter]
    public long? MaxLength { get; set; } = 24;

    [Parameter]
    public string CountrySearchPlaceholder { get; set; } = "Search country";

    [Parameter]
    public string? Class { get; set; }

    public static IReadOnlyList<AdmiralPhoneCountry> DefaultCountries { get; } =
    [
        new("RUS", "Россия", "+7", "admiral:flags/russian-federation"),
        new("BLR", "Беларусь", "+375", "admiral:flags/belarus"),
        new("KAZ", "Казахстан", "+7", "admiral:flags/kazakhstan"),
        new("UZB", "Узбекистан", "+998", "admiral:flags/uzbekistan"),
        new("TJK", "Таджикистан", "+992", "admiral:flags/tajikistan"),
        new("ARM", "Армения", "+374", "admiral:flags/armenia"),
        new("GBR", "United Kingdom", "+44", "admiral:flags/united-kingdom-of-great-britain-and-northern-ireland"),
        new("USA", "United States", "+1", "admiral:flags/united-states-of-america")
    ];

    private IReadOnlyList<AdmiralPhoneCountry> EffectiveCountries => Countries?.ToArray() ?? DefaultCountries;
    private string? currentCountry;
    private string? SelectedIso3 => EffectiveCountries.Any(country => country.Iso3 == currentCountry) ? currentCountry : EffectiveCountries.FirstOrDefault()?.Iso3;
    private AdmiralPhoneCountry? SelectedCountry => EffectiveCountries.FirstOrDefault(country => country.Iso3 == SelectedIso3);
    private string? DisplayValue => ValueMode == AdmiralPhoneValueMode.Digits ? FormatPhone(Value) : Value;
    private string StatusName => Status.ToString().ToLowerInvariant();
    private AdmiralIconSize FlagSize => Size == AdmiralInputSize.Small ? AdmiralIconSize.Medium : AdmiralIconSize.Large;
    private IReadOnlyDictionary<string, object> PhoneInputAttributes => new Dictionary<string, object>
    {
        ["inputmode"] = "tel",
        ["autocomplete"] = "tel"
    };

    protected override void OnParametersSet()
    {
        currentCountry = Country;
    }

    private async Task ChangeCountryAsync(string? iso3)
    {
        var countries = EffectiveCountries;
        var next = countries.FirstOrDefault(country => country.Iso3 == iso3);
        if (next is null)
        {
            return;
        }

        var previous = countries.FirstOrDefault(country => country.Iso3 == SelectedIso3);
        var currentValue = Value ?? string.Empty;
        if (ValueMode == AdmiralPhoneValueMode.Digits)
        {
            currentValue = NormalizePhone(currentValue);
            var previousDial = NormalizePhone(previous?.DialCode);
            if (!string.IsNullOrEmpty(previousDial) && currentValue.StartsWith(previousDial, StringComparison.Ordinal))
            {
                currentValue = currentValue[previousDial.Length..];
            }
        }
        else if (!string.IsNullOrWhiteSpace(previous?.DialCode) && currentValue.StartsWith(previous.DialCode, StringComparison.Ordinal))
        {
            currentValue = currentValue[previous.DialCode.Length..].TrimStart();
        }

        currentCountry = next.Iso3;
        await CountryChanged.InvokeAsync(next.Iso3);
        var nextValue = string.IsNullOrWhiteSpace(currentValue) ? next.DialCode : $"{next.DialCode} {currentValue}";
        await ValueChanged.InvokeAsync(ValueMode == AdmiralPhoneValueMode.Digits ? NormalizePhone(nextValue) : nextValue);
    }

    private async Task HandleValueChanged(string? value)
    {
        var boundValue = ValueMode == AdmiralPhoneValueMode.Digits ? NormalizePhone(value) : value;
        await ValueChanged.InvokeAsync(boundValue);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var comparableValue = NormalizePhone(value);
        var detected = EffectiveCountries
            .Where(country => comparableValue.StartsWith(NormalizePhone(country.DialCode), StringComparison.Ordinal))
            .OrderByDescending(country => country.DialCode.Length)
            // Multiple countries can share a dial code (+7 for RUS and KAZ).
            // Keep the selected country when the matching codes are equally specific.
            .ThenByDescending(country => country.Iso3 == SelectedIso3)
            .FirstOrDefault();
        if (detected is not null && detected.Iso3 != Country)
        {
            currentCountry = detected.Iso3;
            await CountryChanged.InvokeAsync(detected.Iso3);
        }
    }

    private static string NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits;
    }

    private string FormatPhone(string? value)
    {
        var normalized = NormalizePhone(value);
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        var country = EffectiveCountries
            .Where(item => normalized.StartsWith(NormalizePhone(item.DialCode), StringComparison.Ordinal))
            .OrderByDescending(item => item.DialCode.Length)
            .FirstOrDefault(item => item.Iso3 == Country) ??
            EffectiveCountries.Where(item => normalized.StartsWith(NormalizePhone(item.DialCode), StringComparison.Ordinal))
                .OrderByDescending(item => item.DialCode.Length).FirstOrDefault();
        if (country is null)
        {
            return normalized;
        }

        var dial = NormalizePhone(country.DialCode);
        var national = normalized[dial.Length..];
        var groups = country.Iso3 switch
        {
            "RUS" or "KAZ" => new[] { 3, 3, 2, 2 },
            "BLR" => new[] { 2, 3, 2, 2 },
            _ => new[] { 3, 3, 3, 3 }
        };
        var parts = new List<string>();
        var offset = 0;
        foreach (var length in groups)
        {
            if (offset >= national.Length)
            {
                break;
            }
            var take = Math.Min(length, national.Length - offset);
            parts.Add(national.Substring(offset, take));
            offset += take;
        }
        if (offset < national.Length)
        {
            parts.Add(national[offset..]);
        }
        return parts.Count == 0 ? country.DialCode : $"{country.DialCode} {string.Join(' ', parts)}";
    }

    private string CssClass => string.Join(' ', new[]
    {
        "admiral-phone-input",
        $"admiral-phone-input--{(Size == AdmiralInputSize.Small ? "s" : Size == AdmiralInputSize.ExtraLarge ? "xl" : "m")}",
        Status == AdmiralInputStatus.Default ? null : $"admiral-phone-input--{StatusName}",
        Skeleton ? "admiral-phone-input--skeleton" : null,
        Class
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
