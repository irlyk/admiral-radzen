using Radzen.Admiral.Components.Models;
using AdmiralConsumerSmoke.Shared;
using Radzen;

namespace AdmiralConsumerSmoke.Pages;

public partial class Drawer
{
    private string tab = ComponentDocTabs.Admiral;
    private bool open, mobile, preventFocusRestore, longContent;
    private bool backdrop = true, showClose = true, closeOnEscape = true, closeOnBackdrop = true;
    private bool themeOpen, themeSecondary;
    private SidebarPosition themePosition = SidebarPosition.Right;
    private bool themeLeft => themePosition == SidebarPosition.Left;
    private bool themeBackdrop = true;
    private AdmiralDrawerPosition position;
    private AdmiralDrawerAppearance appearance;
    private string width = "30rem", recordName = "Тестовая запись", lastAction = "Панель ещё не открывали";
    private int closeCount, backgroundClicks;

    private void OnCloseRequested()
    {
        closeCount++;
        lastAction = "Пользователь запросил закрытие";
    }

    private void Save()
    {
        lastAction = $"Сохранено: {recordName}";
        open = false;
    }

    private string ThemeBackdropClass => $"admiral-drawer-backdrop admiral-drawer-backdrop--{(themeBackdrop ? "modal" : "nonmodal")}";
    private string ThemeDrawerClass => $"admiral-drawer admiral-drawer--{(themeLeft ? "left" : "right")} admiral-drawer--{(themeSecondary ? "secondary" : "primary")}";
    private string ThemeDrawerStyle => $"position:fixed;top:0;bottom:0;left:{(themeLeft ? "0" : "auto")};right:{(themeLeft ? "auto" : "0")};width:min(30rem,calc(100% - 1rem))";

    private static string T(string ru, string en) => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? ru : en;

    private string PlaygroundCode => $$"""
        <AdmiralButton Text="{{T("Открыть Drawer", "Open Drawer")}}" Click="@(() => open = true)" />
        <span role="status">{{T("Запросов закрытия:", "Close requests:")}} @closeCount</span>
        <AdmiralDrawer @bind-Open="open" Title="{{T("Редактирование записи", "Edit record")}}"
                       Position="AdmiralDrawerPosition.{{position}}"
                       Appearance="AdmiralDrawerAppearance.{{appearance}}"
                       Width="{{width}}" Mobile="{{mobile.ToString().ToLowerInvariant()}}"
                       Backdrop="{{backdrop.ToString().ToLowerInvariant()}}"
                       ShowClose="{{showClose.ToString().ToLowerInvariant()}}"
                       CloseOnEscape="{{closeOnEscape.ToString().ToLowerInvariant()}}"
                       CloseOnBackdropClick="{{closeOnBackdrop.ToString().ToLowerInvariant()}}"
                       PreventFocusRestore="{{preventFocusRestore.ToString().ToLowerInvariant()}}"
                       CloseAriaLabel="{{T("Закрыть Drawer", "Close Drawer")}}" Closed="OnCloseRequested">
            <ChildContent>{{T("Содержимое панели", "Drawer content")}}</ChildContent>
            <Actions><AdmiralButton Text="{{T("Готово", "Done")}}" Click="@(() => open = false)" /></Actions>
        </AdmiralDrawer>
        @code {
            private bool open;
            private int closeCount;
            private void OnCloseRequested() => closeCount++;
        }
        """;

    private const string AdmiralCode = """
        <AdmiralButton Text="Открыть" Click="@(() => open = true)" />
        <AdmiralDrawer @bind-Open="open" Title="Параметры записи"
                       Position="AdmiralDrawerPosition.Left" Width="30rem"
                       Appearance="AdmiralDrawerAppearance.Secondary"
                       CloseAriaLabel="Закрыть панель">
            <ChildContent><AdmiralTextField Label="Название" @bind-Value="name" /></ChildContent>
            <Actions><AdmiralButton Text="Готово" Click="@(() => open = false)" /></Actions>
        </AdmiralDrawer>

        @code {
            private bool open;
            private string name = "";
        }
        """;

    private const string ThemeCode = """
        <RadzenButton Text="Открыть" Click="@(() => open = true)" />
        @if (open)
        {
            <div class="admiral-drawer-backdrop admiral-drawer-backdrop--modal"
                 @onclick="@(() => open = false)">
                <RadzenSidebar Expanded="true" Responsive="false" Position="SidebarPosition.Left"
                               class="admiral-drawer admiral-drawer--left admiral-drawer--secondary"
                               Style="position:fixed;top:0;bottom:0;left:0;right:auto;width:min(30rem,calc(100% - 1rem))"
                               role="dialog" aria-modal="true" aria-labelledby="drawer-title"
                               @onclick:stopPropagation="true">
                    <div class="admiral-drawer__header">
                        <h2 id="drawer-title" class="admiral-drawer__title">Параметры записи</h2>
                    </div>
                    <div class="admiral-drawer__content">Содержимое</div>
                    <div class="admiral-drawer__actions">
                        <RadzenButton Text="Закрыть" Click="@(() => open = false)" />
                    </div>
                </RadzenSidebar>
            </div>
        }
        @code { private bool open; }
        """;

    private static readonly IReadOnlyList<ApiReference.Entry> AdmiralRows =
    [
        new("Open / OpenChanged", "bool / false; EventCallback<bool>", "Управляемая видимость; поддерживает @bind-Open."),
        new("Title", "string / пусто; обязательный", "Заголовок и доступное имя диалога."),
        new("Position", "AdmiralDrawerPosition / Right", "Физический край окна: Left или Right. Mobile всегда использует Right."),
        new("Appearance", "AdmiralDrawerAppearance / Primary", "Primary / Secondary — семантические поверхности текущей Admiral-темы."),
        new("Width", "string / 30rem", "CSS-ширина. Максимум — ширина окна минус 16 px."),
        new("Mobile", "bool / false", "Панель справа шириной окна минус 16 px, уменьшенные отступы и вертикальные действия."),
        new("Backdrop", "bool / true", "Затемнение и блокировка прокрутки страницы. При false фон страницы доступен для мыши."),
        new("ShowClose", "bool / true", "Кнопка IconPlacement: 24 × 24 px и отступы по 4 px, область hover 32 × 32 px."),
        new("CloseOnEscape", "bool / true", "Запрос закрытия по Escape, когда фокус внутри панели."),
        new("CloseOnBackdropClick", "bool / true", "Запрос закрытия по нажатию на подложку; работает только при Backdrop=true."),
        new("PreventFocusRestore", "bool / false", "При true не возвращает фокус на элемент, открывший панель."),
        new("CloseAriaLabel", "string / Close drawer", "Локализуемая подпись кнопки закрытия."),
        new("Closed", "EventCallback / —", "Уведомление о запросе закрытия из UI, после OpenChanged(false)."),
        new("ChildContent / Actions", "RenderFragment / null", "Прокручиваемое содержимое и неподвижная панель действий."),
        new("Class / AdditionalAttributes", "string / null; IReadOnlyDictionary<string, object> / null", "Дополнительные классы и атрибуты dialog, например data-testid.")
    ];

    private static readonly IReadOnlyList<ApiReference.Entry> ThemeRows =
    [
        new("Expanded / Responsive", "bool / true / false в примере", "Панель раскрыта; автоматическое поведение навигационного sidebar отключено."),
        new("Position", "SidebarPosition / Left или Right", "Позиция Radzen согласуется с CSS-модификатором панели."),
        new("admiral-drawer--left / --right", "CSS-классы", "Край окна и направление анимации открытия."),
        new("admiral-drawer--secondary / --mobile", "CSS-классы", "Вторичная поверхность / компактная геометрия."),
        new("admiral-drawer-backdrop--modal / --nonmodal", "CSS-классы", "Затемнение с блокировкой страницы / доступный фон без затемнения."),
        new("admiral-drawer__header / __content / __actions", "CSS-классы", "Заголовок, независимая прокрутка и панель действий."),
        new("Style / --admiral-drawer-width", "CSS / 30rem", "Явная позиция и ширина заменяют стандартные inline-отступы RadzenSidebar.")
    ];
}
