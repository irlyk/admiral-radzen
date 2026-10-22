using System.Globalization;

namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralDataGridLocalization
{
    public string Search { get; init; } = "Search";
    public string OpenSearch { get; init; } = "Open search";
    public string CloseSearch { get; init; } = "Close search";
    public string ConfigureColumns { get; init; } = "Configure columns";
    public string VisibleColumns { get; init; } = "Visible columns";
    public string Settings { get; init; } = "Table settings";
    public string Actions { get; init; } = "Actions";
    public string GroupPanel { get; init; } = "Drag a column header here to group";
    public string Filter { get; init; } = "Filter";
    public string Select { get; init; } = "Select...";
    public string NoValue { get; init; } = "No value";
    public string Apply { get; init; } = "Apply";
    public string Clear { get; init; } = "Clear";
    public string And { get; init; } = "And";
    public string Or { get; init; } = "Or";
    public string EqualsOperator { get; init; } = "Equals";
    public string NotEquals { get; init; } = "Not equals";
    public string LessThan { get; init; } = "Less than";
    public string LessThanOrEquals { get; init; } = "Less than or equals";
    public string GreaterThan { get; init; } = "Greater than";
    public string GreaterThanOrEquals { get; init; } = "Greater than or equals";
    public string StartsWith { get; init; } = "Starts with";
    public string EndsWith { get; init; } = "Ends with";
    public string Contains { get; init; } = "Contains";
    public string DoesNotContain { get; init; } = "Does not contain";
    public string In { get; init; } = "In";
    public string NotIn { get; init; } = "Not in";
    public string IsNull { get; init; } = "Is null";
    public string IsNotNull { get; init; } = "Is not null";
    public string IsEmpty { get; init; } = "Is empty";
    public string IsNotEmpty { get; init; } = "Is not empty";
    public string Custom { get; init; } = "Custom";
    public string NoRecords { get; init; } = "No records to display.";
    public string RemoveGroup { get; init; } = "Remove group";
    public string LogicalOperatorAriaLabel { get; init; } = "logical filter operator";
    public string FilterOperatorAriaLabel { get; init; } = "filter operator";
    public string SecondFilterOperatorAriaLabel { get; init; } = "second filter operator";
    public string FilterValueAriaLabel { get; init; } = "filter value";
    public string SecondFilterValueAriaLabel { get; init; } = "second filter value";
    public string ExpandAll { get; init; } = "Expand all";
    public string CollapseAll { get; init; } = "Collapse all";
    public string FirstPage { get; init; } = "First page";
    public string PreviousPage { get; init; } = "Previous page";
    public string NextPage { get; init; } = "Next page";
    public string LastPage { get; init; } = "Last page";
    public string PageFormat { get; init; } = "Page {0}";
    public string GoToPageFormat { get; init; } = "Go to page {0}";
    public string ItemsPerPage { get; init; } = "Items per page";
    public string ItemRangeFormat { get; init; } = "{0}–{1} of {2} items";
    public string PageRangeFormat { get; init; } = "of {0} pages";

    public static AdmiralDataGridLocalization ForCurrentCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? Russian
            : new();

    public static AdmiralDataGridLocalization Russian { get; } = new()
    {
        Search = "Поиск",
        OpenSearch = "Открыть поиск",
        CloseSearch = "Закрыть поиск",
        ConfigureColumns = "Настроить столбцы",
        VisibleColumns = "Видимые столбцы",
        Settings = "Настройки таблицы",
        Actions = "Действия",
        GroupPanel = "Перетащите заголовок столбца сюда для группировки",
        Filter = "Фильтр",
        Select = "Выберите...",
        NoValue = "Нет значения",
        Apply = "Применить",
        Clear = "Очистить",
        And = "И",
        Or = "Или",
        EqualsOperator = "Равно",
        NotEquals = "Не равно",
        LessThan = "Меньше",
        LessThanOrEquals = "Меньше или равно",
        GreaterThan = "Больше",
        GreaterThanOrEquals = "Больше или равно",
        StartsWith = "Начинается с",
        EndsWith = "Заканчивается на",
        Contains = "Содержит",
        DoesNotContain = "Не содержит",
        In = "В списке",
        NotIn = "Не в списке",
        IsNull = "Пусто",
        IsNotNull = "Не пусто",
        IsEmpty = "Пустая строка",
        IsNotEmpty = "Непустая строка",
        Custom = "Другое",
        NoRecords = "Нет данных.",
        RemoveGroup = "Удалить группировку",
        LogicalOperatorAriaLabel = "логический оператор фильтра",
        FilterOperatorAriaLabel = "оператор фильтра",
        SecondFilterOperatorAriaLabel = "второй оператор фильтра",
        FilterValueAriaLabel = "значение фильтра",
        SecondFilterValueAriaLabel = "второе значение фильтра",
        ExpandAll = "Развернуть все",
        CollapseAll = "Свернуть все",
        FirstPage = "Первая страница",
        PreviousPage = "Предыдущая страница",
        NextPage = "Следующая страница",
        LastPage = "Последняя страница",
        PageFormat = "Страница {0}",
        GoToPageFormat = "Перейти на страницу {0}",
        ItemsPerPage = "Строк на странице",
        ItemRangeFormat = "{0}–{1} из {2}",
        PageRangeFormat = "из {0} страниц"
    };
}
