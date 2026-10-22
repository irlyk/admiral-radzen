using Microsoft.AspNetCore.Components;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

/// <summary>Compatible integer slider. Use AdmiralNumericSlider for decimal values and formatted marks.</summary>
public partial class AdmiralSlider
{
    [Parameter]
    public int Value { get; set; }
    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }
    [Parameter]
    public IEnumerable<int> Values { get; set; } = [0, 100];
    [Parameter]
    public EventCallback<IEnumerable<int>> ValuesChanged { get; set; }
    /// <summary>Pointer completion after the final integer value callback.</summary>
    [Parameter]
    public EventCallback<AdmiralSliderDragEndEventArgs> DragEnded { get; set; }
    [Parameter]
    public bool Range { get; set; }
    [Parameter]
    public int Min { get; set; }
    [Parameter]
    public int Max { get; set; } = 100;
    [Parameter]
    public int Step { get; set; } = 1;
    [Parameter]
    public IReadOnlyList<int>? Marks { get; set; }
    [Parameter]
    public AdmiralSliderDimension Dimension { get; set; } = AdmiralSliderDimension.Xl;
    [Parameter]
    public bool ShowTooltip { get; set; }
    [Parameter]
    public bool Disabled { get; set; }
    [Parameter]
    public bool Skeleton { get; set; }
    [Parameter]
    public string AriaLabel { get; set; } = "Slider";
    [Parameter]
    public string MinHandleAriaLabel { get; set; } = "Minimum value";
    [Parameter]
    public string MaxHandleAriaLabel { get; set; } = "Maximum value";
    [Parameter]
    public string? Class { get; set; }
    [Parameter]
    public string? Style { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private Task HandleValueChanged(decimal value) => ValueChanged.InvokeAsync(decimal.ToInt32(value));
    private Task HandleValuesChanged(IEnumerable<decimal> values) => ValuesChanged.InvokeAsync(values.Select(decimal.ToInt32).ToArray());
}
