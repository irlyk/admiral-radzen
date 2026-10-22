using Radzen.Admiral.Components.Models;
using Radzen.Admiral.Components;
using AdmiralConsumerSmoke.Shared;

namespace AdmiralConsumerSmoke.Pages;

public partial class Slider
{
    private string tab = ComponentDocTabs.Admiral;
    private AdmiralSliderDimension dimension = AdmiralSliderDimension.Xl;
    private decimal value = 5.25m, radzenValue = 3.5m;
    private decimal offsetValue = 0.3m;
    private IEnumerable<decimal> rangeValues = [2.25m, 7.75m];
    private readonly decimal[] marks = [0, 2.5m, 5, 7.5m, 10];
    private int legacyValue = 50, changes, dragEnds;
    private bool anyStep, lastCancelled, completionMatched;
    private AdmiralNumericSlider sliderControl = default!;
    private async Task ResetValueAsync()
    {
        await sliderControl.CancelInteractionAsync();
        value = 5.25m;
        rangeValues = [2.25m, 7.75m];
    }
    private async Task CoincideAsync()
    {
        await sliderControl.CancelInteractionAsync();
        range = true;
        rangeValues = [5m, 5m];
    }
    private static readonly IReadOnlyDictionary<string, object> HandleData = new Dictionary<string, object> { ["data-testid"] = "single-handle", ["title"] = "Настраиваемый HTML-атрибут", ["class"] = "sample-slider-handle" };
    private static readonly IReadOnlyDictionary<string, object> LowerData = new Dictionary<string, object> { ["data-testid"] = "lower-handle" };
    private static readonly IReadOnlyDictionary<string, object> UpperData = new Dictionary<string, object> { ["data-testid"] = "upper-handle" };
    private readonly int[] legacyMarks = [0, 25, 50, 75, 100];
    private bool range, showMarks = true, showTooltip = true, disabled, skeleton, customMarks, radzenDisabled, radzenSmall = true;
    private string cultureName = "ru-RU";
    private System.Globalization.CultureInfo culture => System.Globalization.CultureInfo.GetCultureInfo(cultureName);
    private void OnValueChanged(decimal next) { value = next; changes++; }
    private void OnValuesChanged(IEnumerable<decimal> next) { rangeValues = next.ToArray(); changes++; }
    private void OnDragEnded(AdmiralSliderDragEndEventArgs args)
    {
        dragEnds++;
        lastCancelled = args.Cancelled;
        completionMatched = args.Values.SequenceEqual(range ? rangeValues : [value]);
    }
    private const string AdmiralCode = """
        <AdmiralNumericSlider @bind-Value="amount" Min="0" Max="10" Step="0.25m"
                              Marks="@marks" ShowTooltip="true" />

        <AdmiralNumericSlider Range="true" @bind-Values="interval" Min="0" Max="10" Step="0.25m"
                              AnyStep="true" DragEnded="OnDragEnded" />
        @code {
            decimal amount = 5.25m;
            IEnumerable<decimal> interval = [2.25m, 7.75m];
            decimal[] marks = [0, 2.5m, 5, 7.5m, 10];
            void OnDragEnded(AdmiralSliderDragEndEventArgs args) { /* args.Values, args.Cancelled */ }
        }
        """;
    private const string ThemeCode = """
        <RadzenSlider TValue="decimal" @bind-Value="amount" Min="0" Max="10"
                      Step="0.25" HandleLabel="Громкость" Style="width:100%" class="admiral-slider--medium" />
        @code { decimal amount = 3.5m; }
        """;
    private static readonly IReadOnlyList<ApiReference.Entry> RadzenRows =
    [
        new("Value / ValueChanged", "decimal / EventCallback<decimal>", "Значение одинарного слайдера."),
        new("Range", "bool = false", "Включает два маркера; тогда TValue — IEnumerable<decimal>."),
        new("Min / Max / Step", "decimal / decimal / string", "Границы и шаг RadzenSlider."),
        new("class", "string", "admiral-slider--medium для маркера M; без класса — XL.")
    ];
    private static readonly IReadOnlyList<ApiReference.Entry> NumericRows =
    [
        new("Value / ValueChanged", "decimal = 0 / EventCallback<decimal>", "Одинарное значение и уведомление об изменении."),
        new("Values / ValuesChanged", "IEnumerable<decimal>? = null / EventCallback<IEnumerable<decimal>>", "Два значения; null использует границы, обратный порядок сортируется."),
        new("Range", "bool = false", "Два ползунка вместо одного."),
        new("Min / Max / Step", "decimal = 0 / 100 / 1", "Границы и положительный шаг; Max > Min."),
        new("AnyStep", "bool = false", "Свободный pointer без округления; клавиатура всё равно использует положительный Step."),
        new("DragEnded", "EventCallback<AdmiralSliderDragEndEventArgs>", "Один callback после pointerup/cancel, после финального изменения. Values — снимок пары/значения, Cancelled отличает отмену."),
        new("AdmiralSliderDragEndEventArgs", "Value / Values / Cancelled", "Value — значение или нижняя граница Range; Values — независимый снимок из 1/2 значений; Cancelled=true для pointercancel/lost capture."),
        new("CancelInteractionAsync()", "Task / метод", "Отбрасывает preview и ожидающие события без DragEnded; используйте перед сбросом к тому же bound value."),
        new("HandleAttributes / MinHandleAttributes / MaxHandleAttributes", "IReadOnlyDictionary<string, object>? = null", "Дополнительные атрибуты одного/нижнего/верхнего маркера. Встроенные role, style, tabindex и value ARIA сохраняются."),
        new("Marks / ShowMarks", "IReadOnlyList<decimal>? = null / bool = true", "Список дополнительных отметок и их видимость. Границы добавляются автоматически."),
        new("MarkTemplate", "RenderFragment<decimal>? = null", "Произвольный локализованный текст отметки."),
        new("Culture / ValueFormat", "CultureInfo? = null / string = 0.##", "Форматирование отметок и значений; null означает CurrentCulture."),
        new("Dimension", "AdmiralSliderDimension = Xl", "XL: 20px, M: 16px. Для React Range это M/S."),
        new("ShowTooltip / Disabled / Skeleton", "bool = false", "Подсказка значения; запрет взаимодействия; загрузочный placeholder."),
        new("AriaLabel / MinHandleAriaLabel / MaxHandleAriaLabel", "string = Slider / Minimum value / Maximum value", "Локализуемые доступные имена маркеров."),
        new("Class / Style / AdditionalAttributes", "string? / string? / IReadOnlyDictionary<string, object>? = null", "Классы, CSS и атрибуты корневого элемента.")
    ];
}
