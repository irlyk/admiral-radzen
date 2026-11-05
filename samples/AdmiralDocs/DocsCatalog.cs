namespace AdmiralConsumerSmoke;

internal static class DocsCatalog
{
    internal sealed record Entry(string Name, string Href, string? Russian = null)
    {
        public string Label(bool russian) => russian ? Russian ?? Name : Name;
    }

    internal sealed record Group(string English, string Russian, Entry[] Entries)
    {
        public string Label(bool russian) => russian ? Russian : English;
    }

    internal static readonly Group[] Groups =
    [
        new("Basics", "Основы",
        [
            new("Getting started", "guide/getting-started", "Начало работы"),
            new("Colors", "basics/colors", "Цвета"),
            new("Typography", "components/typography", "Типографика"),
            new("Integration", "guide/integration", "Подключение"),
            new("Icons", "icons", "Иконки"),
            new("Layout", "layout", "Макет"),
        ]),
        new("Actions", "Действия",
        [
            new("Button", "components/button"),
            new("IconButton", "components/icon-button"),
            new("TextButton", "components/text-button"),
            new("ButtonGroup", "components/button-groups"),
            new("IconButtonGroup", "components/icon-button-group"),
            new("FloatingButtonMenu", "components/floating-button-menu"),
            new("ActionBar / GroupActionsPane", "components/action-bar"),
        ]),
        new("Inputs", "Поля ввода",
        [
            new("Switch", "components/switch"),
            new("Checkbox", "components/checkbox"),
            new("Checkbox groups", "components/checkbox-group", "Группы Checkbox"),
            new("TextInput", "components/text-input"),
            new("TextArea", "components/text-area"),
            new("NumberInput", "components/number-input"),
            new("Select", "components/select"),
            new("InputField", "components/input-field"),
            new("DropDown", "components/drop-down"),
            new("DateRangePicker", "components/date-range-picker"),
            new("TreeSelect", "components/tree-select"),
            new("Slider / Range", "components/slider"),
            new("PhoneNumberInput", "components/phone-number-input"),
            new("GlobalSearch", "components/global-search"),
        ]),
        new("Navigation", "Навигация",
        [
            new("Link", "components/link"),
            new("Menu", "components/menu"),
            new("Drawer", "components/drawer"),
            new("Stepper", "components/stepper"),
            new("Tree", "components/tree"),
            new("Pagination", "components/pagination"),
        ]),
        new("Data Display", "Отображение данных",
        [
            new("Divider", "components/divider"),
            new("Badge", "components/badge"),
            new("Chips", "components/chips"),
            new("Pills", "components/pills"),
            new("Tag", "components/tag"),
            new("Lists", "components/lists"),
            new("ScrollContainer", "components/scroll-container"),
            new("Carousel", "components/carousel"),
            new("ImageViewer", "components/image-viewer"),
            new("Table", "tables"),
            new("Avatar", "components/avatar"),
            new("StatusIndicator", "components/status-indicator"),
            new("Grid", "components/grid"),
        ]),
        new("Feedback", "Обратная связь",
        [
            new("Skeleton", "components/skeleton"),
            new("Tooltip", "components/tooltip"),
            new("Hint", "components/hint"),
            new("Toast", "components/toast"),
            new("Progress", "components/progress"),
            new("Spinner", "components/spinner"),
        ]),
    ];
}
