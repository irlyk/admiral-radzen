using AdmiralConsumerSmoke.Shared;
using Radzen;
using Radzen.Blazor;
using Radzen.Admiral.Components.Models;

namespace AdmiralConsumerSmoke.Pages;

public partial class Hint
{
    private string tab = ComponentDocTabs.Admiral;
    private AdmiralHintPlacement placement = AdmiralHintPlacement.Auto;
    private AdmiralHintSize size = AdmiralHintSize.Medium;
    private AdmiralHintTrigger trigger = AdmiralHintTrigger.Click;
    private bool open, disabled, preventFocusSteal, longContent;
    private bool outside = true;
    private int changes, actions;
    private RadzenButton? themeTrigger;
    private TooltipPosition themePosition = TooltipPosition.Bottom;
    private string themeSize = "medium";

    private void OnChanged(bool value) { open = value; changes++; }
    private void OpenThemeHint()
    {
        if (themeTrigger is null) return;
        Tooltips.Open(themeTrigger.Element, service => ThemeContent(service), new TooltipOptions
        {
            Position = themePosition, Duration = null, CloseTooltipOnDocumentClick = true,
            CssClass = $"admiral-hint admiral-hint--{themeSize}"
        });
    }

    private static string T(string ru, string en) => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? ru : en;

    private string PlaygroundUsage => $$"""
        <AdmiralHint TriggerText="{{T("Открыть Hint", "Open Hint")}}" Title="{{T("Обработка записи", "Record processing")}}"
                     @bind-Open="open" Placement="AdmiralHintPlacement.{{placement}}"
                     Size="AdmiralHintSize.{{size}}" VisibilityTrigger="AdmiralHintTrigger.{{trigger}}"
                     Disabled="{{disabled.ToString().ToLowerInvariant()}}"
                     PreventFocusSteal="{{preventFocusSteal.ToString().ToLowerInvariant()}}"
                     CloseOnDocumentClick="{{outside.ToString().ToLowerInvariant()}}"
                     TriggerAppearance="AdmiralButtonAppearance.Secondary" CloseAriaLabel="{{T("Закрыть Hint", "Close Hint")}}">
            {{T("Содержимое подсказки", "Hint content")}}
        </AdmiralHint>
        @code { private bool open; }
        """;
    private const string Usage = """
        <AdmiralHint Title="Сведения о записи" Placement="AdmiralHintPlacement.Auto"
                     Size="AdmiralHintSize.Large" CloseAriaLabel="Закрыть сведения">
            <TriggerContent>
                <AdmiralIconButton Icon="admiral:service/info-outline" AriaLabel="Сведения о записи" />
            </TriggerContent>
            <ChildContent>Сведения и действия</ChildContent>
        </AdmiralHint>
        """;
    private const string ThemeUsage = """
        @inject TooltipService Tooltips
        <RadzenButton @ref="button" Text="Открыть" Click="Show" />
        @code {
            private RadzenButton? button;
            private void Show() => Tooltips.Open(button!.Element, "Подробная подсказка",
                new TooltipOptions {
                    Position = TooltipPosition.Bottom, Duration = null,
                    CloseTooltipOnDocumentClick = true,
                    CssClass = "admiral-hint admiral-hint--medium"
                });
        }
        """;
    private static readonly IReadOnlyList<ApiReference.Entry> Parameters =
    [
        new("TriggerText / TriggerIcon", "string / пусто; help-outline", "Текст и иконка стандартного триггера."),
        new("TriggerContent", "RenderFragment / null", "Произвольный фокусируемый триггер; имеет приоритет над стандартным."),
        new("Title / ChildContent", "string / пусто; RenderFragment / null", "Доступный заголовок и содержимое карточки."),
        new("Size", "AdmiralHintSize / Medium", "Small 280, Medium 384, Large 488 px. На мобильном ширина адаптивна."),
        new("Placement", "AdmiralHintPlacement? / null", "Auto, Top, Right, Bottom, Left с подбором безопасного положения. Null использует Position."),
        new("VisibilityTrigger", "AdmiralHintTrigger / Click", "Click — явное закрытие и focus trap. Hover — hover/focus без перемещения фокуса."),
        new("Open / OpenChanged", "bool / false; EventCallback<bool>", "Видимость, двусторонняя привязка и наблюдаемый результат открытия/закрытия."),
        new("PreventFocusSteal", "bool / false", "Отключает начальный фокус и focus trap в click-режиме."),
        new("TriggerAppearance", "AdmiralButtonAppearance? / null", "Вариант Admiral-кнопки; null сохраняет старые параметры Radzen."),
        new("Disabled", "bool / false", "Запрещает открытие и скрывает открытую карточку."),
        new("CloseOnDocumentClick", "bool / true", "Закрытие click-подсказки вне триггера и карточки."),
        new("CloseAriaLabel", "string / Close hint", "Локализуемое доступное имя крестика."),
        new("Position", "TooltipPosition / Bottom", "Совместимость: используйте Admiral Placement в новом коде."),
        new("TriggerButtonStyle / TriggerVariant", "ButtonStyle / Primary; Variant / Filled", "Совместимость: заменяются TriggerAppearance или TriggerContent."),
        new("Class / AdditionalAttributes", "string / null; словарь / null", "Передаются стандартной кнопке. При TriggerContent атрибуты задаются самому триггеру.")
    ];
    private static readonly IReadOnlyList<ApiReference.Entry> ThemeParameters =
    [
        new("admiral-hint", "CSS-класс TooltipOptions.CssClass", "Поверхность, радиус, тень и типографика карточки."),
        new("admiral-hint--small / --medium / --large", "CSS-модификатор", "Ширины 280 / 384 / 488 px, адаптивная мобильная ширина."),
        new("admiral-hint__dialog / __content / __title / __close", "CSS-классы", "Композиция интерактивного содержимого и кнопки закрытия."),
        new("Position / Duration / CloseTooltipOnDocumentClick", "Radzen TooltipOptions", "Позиция, время жизни и закрытие снаружи. Фокусом и hover-переходами тема не управляет.")
    ];
}
