using System.Globalization;
using Microsoft.AspNetCore.Components;
using Radzen.Admiral.Components.Models;
using Microsoft.JSInterop;

namespace Radzen.Admiral.Components;

/// <summary>Admiral single-value or range slider with decimal values and localized marks.</summary>
public partial class AdmiralNumericSlider
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>Single value, zero by default. Display is clamped to the bounds.</summary>
    [Parameter]
    public decimal Value { get; set; }

    /// <summary>Raised when the single value changes through pointer, keyboard or a mark.</summary>
    [Parameter]
    public EventCallback<decimal> ValueChanged { get; set; }

    /// <summary>Range endpoints. Missing endpoints use Min and Max; reversed endpoints are sorted.</summary>
    [Parameter]
    public IEnumerable<decimal>? Values { get; set; }

    /// <summary>Raised with a new two-element array when range endpoints change.</summary>
    [Parameter]
    public EventCallback<IEnumerable<decimal>> ValuesChanged { get; set; }

    /// <summary>Whether to show two handles. Defaults to false.</summary>
    [Parameter]
    public bool Range { get; set; }

    /// <summary>Lower bound, zero by default.</summary>
    [Parameter]
    public decimal Min { get; set; }

    /// <summary>Upper bound, 100 by default. Must exceed Min.</summary>
    [Parameter]
    public decimal Max { get; set; } = 100;

    /// <summary>Positive step, one by default. Fractional values are supported.</summary>
    [Parameter]
    public decimal Step { get; set; } = 1;

    /// <summary>Continuous pointer values without snapping. Step still controls keyboard increments.</summary>
    [Parameter]
    public bool AnyStep { get; set; }

    /// <summary>Called once after pointer release/cancel, after the final value callback. Not raised for keyboard/marks.</summary>
    [Parameter]
    public EventCallback<AdmiralSliderDragEndEventArgs> DragEnded { get; set; }

    /// <summary>Additional single-handle HTML attributes. Built-in role, style, tabindex and value ARIA remain authoritative.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, object>? HandleAttributes { get; set; }

    /// <summary>Additional lower-handle HTML attributes in range mode.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, object>? MinHandleAttributes { get; set; }

    /// <summary>Additional upper-handle HTML attributes in range mode.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, object>? MaxHandleAttributes { get; set; }

    /// <summary>Optional marks, filtered to the bounds and sorted. Bounds are always included.</summary>
    [Parameter]
    public IReadOnlyList<decimal>? Marks { get; set; }

    /// <summary>Displays clickable marks and endpoints. Defaults to true.</summary>
    [Parameter]
    public bool ShowMarks { get; set; } = true;

    /// <summary>Optional localized mark rendering. Receives the original numeric value.</summary>
    [Parameter]
    public RenderFragment<decimal>? MarkTemplate { get; set; }

    /// <summary>Numeric format for default marks and tooltips, 0.## by default.</summary>
    [Parameter]
    public string ValueFormat { get; set; } = "0.##";

    /// <summary>Formatting culture; null uses CurrentCulture.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Thumb size XL (20px, default) or M (16px).</summary>
    [Parameter]
    public AdmiralSliderDimension Dimension { get; set; } = AdmiralSliderDimension.Xl;

    /// <summary>Shows the value on thumb hover, keyboard focus or drag. Defaults to false.</summary>
    [Parameter]
    public bool ShowTooltip { get; set; }

    /// <summary>Disables all handles and marks. Defaults to false.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows a non-interactive loading placeholder. Defaults to false.</summary>
    [Parameter]
    public bool Skeleton { get; set; }

    /// <summary>Accessible name of the single handle.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Slider";

    /// <summary>Accessible name of the lower handle.</summary>
    [Parameter]
    public string MinHandleAriaLabel { get; set; } = "Minimum value";

    /// <summary>Accessible name of the upper handle.</summary>
    [Parameter]
    public string MaxHandleAriaLabel { get; set; } = "Maximum value";

    /// <summary>Additional root CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Additional root CSS declarations.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>Additional root HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private decimal currentValue;
    private decimal[] endpoints = [0, 100];
    private decimal[] visibleMarks = [];
    private ElementReference root;
    private IJSObjectReference? interactionModule, interaction;
    private DotNetObjectReference<AdmiralNumericSlider>? selfReference;
    private readonly SemaphoreSlim renderGate = new(1, 1);
    private bool disposed;
    private int configurationVersion;
    private (decimal Min, decimal Max, decimal Step, bool Any, bool Range, bool Blocked)? previousConfiguration;
    private static string Invariant(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private IReadOnlyDictionary<string, object>? HandleAttributesFor(int index) => !Range ? HandleAttributes : index == 0 ? MinHandleAttributes : MaxHandleAttributes;
    private string HandleCssClass(int index)
    {
        var attributes = HandleAttributesFor(index);
        var extraClass = attributes is not null && attributes.TryGetValue("class", out var value) ? value?.ToString() : null;
        return $"rz-slider-handle rz-slider-handle-horizontal {extraClass}";
    }
    private string CssClass => $"admiral-slider admiral-slider--{Dimension.ToString().ToLowerInvariant()} {(Range ? "admiral-slider--range" : null)} {(ShowTooltip ? "admiral-slider--tooltip" : null)} {(Disabled ? "admiral-slider--disabled" : null)} {(Skeleton ? "admiral-slider--skeleton" : null)} {Class}";
    private string CombinedStyle => $"--admiral-slider-value:{CssString(Format(currentValue))};--admiral-slider-start:{CssString(Format(endpoints[0]))};--admiral-slider-end:{CssString(Format(endpoints[1]))};{Style}";
    private string FillStyle => $"inset-inline-start:var(--admiral-slider-fill-start,{(Range ? Percent(endpoints[0]) : "0")}%);width:var(--admiral-slider-fill-width,{(Range ? Invariant((endpoints[1] - endpoints[0]) / (Max - Min) * 100) : Percent(currentValue))}%)";
    private string PositionStyle(int index) => $"inset-inline-start:var(--admiral-slider-position-{index},{Percent(Range ? endpoints[index] : currentValue)}%)";

    /// <summary>Discard pending pointer/key preview without DragEnded. Use before resetting to the same bound value.</summary>
    public Task CancelInteractionAsync() => InvokeAsync(() =>
    {
        if (disposed) return;
        configurationVersion++;
        StateHasChanged();
    });

    protected override void OnParametersSet()
    {
        if (Max <= Min)
        {
            throw new ArgumentOutOfRangeException(nameof(Max), "Max must exceed Min.");
        }
        if (Step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Step), "Step must be positive.");
        }
        var nextValue = Normalize(Value);
        var values = Values?.Take(2).ToArray() ?? [];
        decimal[] nextEndpoints = [Normalize(values.Length > 0 ? values[0] : Min), Normalize(values.Length > 1 ? values[1] : Max)];
        Array.Sort(nextEndpoints);
        var externallyChanged = Range ? !endpoints.SequenceEqual(nextEndpoints) : currentValue != nextValue;
        currentValue = nextValue;
        endpoints = nextEndpoints;
        visibleMarks = (Marks ?? []).Append(Min).Append(Max).Where(mark => mark >= Min && mark <= Max).Distinct().Order().ToArray();
        var configuration = (Min, Max, Step, AnyStep, Range, Disabled || Skeleton);
        if (previousConfiguration != configuration || externallyChanged)
        {
            configurationVersion++;
            previousConfiguration = configuration;
        }
    }

    private decimal Normalize(decimal value)
    {
        value = Math.Clamp(value, Min, Max);
        if (AnyStep || value == Min || value == Max)
        {
            return value;
        }
        return Math.Clamp(Min + Math.Round((value - Min) / Step, MidpointRounding.AwayFromZero) * Step, Min, Max);
    }

    private string Format(decimal value) => value.ToString(ValueFormat, Culture ?? CultureInfo.CurrentCulture);
    private static string CssString(string value) => "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ") + "'";
    private string Percent(decimal mark) => Invariant((mark - Min) / (Max - Min) * 100);

    [JSInvokable]
    public async Task<object> UpdateFromBrowser(decimal[] values, int version, bool finished, bool cancelled)
    {
        await InvokeAsync(async () =>
        {
            if (disposed || Disabled || Skeleton || version != configurationVersion || values.Length != (Range ? 2 : 1))
            {
                return;
            }
            if (Range)
            {
                var next = values.Select(Normalize).Order().ToArray();
                if (!endpoints.SequenceEqual(next))
                {
                    endpoints = next;
                    await ValuesChanged.InvokeAsync(next.ToArray());
                }
            }
            else
            {
                var next = Normalize(values[0]);
                if (next != currentValue)
                {
                    currentValue = next;
                    await ValueChanged.InvokeAsync(next);
                }
            }
            if (finished && version == configurationVersion && !disposed)
            {
                await DragEnded.InvokeAsync(new(Range ? endpoints[0] : currentValue, Range ? endpoints.ToArray() : [currentValue], cancelled));
            }
            if (!disposed) StateHasChanged();
        });
        return new { values = Range ? endpoints.ToArray() : [currentValue], version = configurationVersion };
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await renderGate.WaitAsync();
        try
        {
            if (disposed) return;
            interactionModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-slider.js");
            selfReference ??= DotNetObjectReference.Create(this);
            interaction ??= await interactionModule.InvokeAsync<IJSObjectReference>("connect", root, selfReference);
            await interaction.InvokeVoidAsync("update", new { min = Min, max = Max, step = Step, any = AnyStep, range = Range,
                blocked = Disabled || Skeleton, version = configurationVersion, values = Range ? endpoints : [currentValue] });
        }
        finally { renderGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        await renderGate.WaitAsync();
        try
        {
            if (interaction is not null)
            {
                await interaction.InvokeVoidAsync("dispose");
                await interaction.DisposeAsync();
            }
            if (interactionModule is not null) await interactionModule.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        finally { selfReference?.Dispose(); renderGate.Release(); }
    }

    private Task SelectMark(decimal mark)
    {
        if (Disabled || Skeleton)
        {
            return Task.CompletedTask;
        }
        mark = Normalize(mark);
        configurationVersion++; // Invalidate any older queued pointer/key preview before a mark action.
        if (!Range)
        {
            currentValue = mark;
            return ValueChanged.InvokeAsync(mark);
        }
        endpoints = Math.Abs(mark - endpoints[0]) <= Math.Abs(mark - endpoints[1])
            ? [Math.Min(mark, endpoints[1]), endpoints[1]] : [endpoints[0], Math.Max(mark, endpoints[0])];
        return ValuesChanged.InvokeAsync(endpoints.ToArray());
    }
}
