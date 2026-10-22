using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Admiral.Components.Models;
using Radzen.Blazor;

namespace Radzen.Admiral.Components;

public partial class AdmiralDataGrid<TItem> : IAsyncDisposable where TItem : notnull
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired]
    public IEnumerable<TItem> Data { get; set; } = [];

    [Parameter]
    public IReadOnlyList<AdmiralDataGridColumnDefinition<TItem>> Columns { get; set; } = [];

    [Parameter]
    public RenderFragment? ColumnContent { get; set; }

    [Parameter]
    public IReadOnlyList<AdmiralDataGridAction<TItem>> RowActions { get; set; } = [];

    [Parameter]
    public EventCallback<AdmiralDataGridActionEventArgs<TItem>> RowActionClicked { get; set; }

    [Parameter]
    public RenderFragment<TItem>? Actions { get; set; }

    [Parameter]
    public IReadOnlyList<AdmiralActionItem> ToolbarActions { get; set; } = [];

    [Parameter]
    public EventCallback<AdmiralActionItem> ToolbarActionClicked { get; set; }

    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    [Parameter]
    public bool ShowSettings { get; set; }

    [Parameter]
    public EventCallback SettingsClicked { get; set; }

    [Parameter]
    public string SearchValue { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchValueChanged { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<AdmiralPaneColumn>> VisibleColumnsChanged { get; set; }

    [Parameter]
    public Func<TItem, string, bool>? SearchPredicate { get; set; }

    [Parameter]
    public AdmiralDataGridLocalization? Localization { get; set; }

    [Parameter]
    public AdmiralDataGridSize Size { get; set; } = AdmiralDataGridSize.Medium;

    [Parameter]
    public AdmiralDataGridPagination Pagination { get; set; } = AdmiralDataGridPagination.PaginationTwo;

    [Parameter]
    public AdmiralPaginationSize PaginationSize { get; set; } = AdmiralPaginationSize.Medium;

    [Parameter]
    public bool AllowPaging { get; set; } = true;

    [Parameter]
    public int PageSize { get; set; } = 10;

    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public EventCallback<LoadDataArgs> LoadData { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    [Parameter]
    public EventCallback<PagerEventArgs> Page { get; set; }

    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 20, 50, 100];

    [Parameter]
    public bool AllowSorting { get; set; } = true;

    [Parameter]
    public bool AllowMultiColumnSorting { get; set; }

    [Parameter]
    public bool AllowFiltering { get; set; }

    [Parameter]
    public FilterMode FilterMode { get; set; } = FilterMode.Advanced;

    [Parameter]
    public bool AllowColumnResize { get; set; }

    [Parameter]
    public bool AllowColumnReorder { get; set; }

    [Parameter]
    public bool AllowColumnPicking { get; set; }

    [Parameter]
    public bool AllowPickAllColumns { get; set; } = true;

    [Parameter]
    public bool ColumnsPickerAllowFiltering { get; set; }

    [Parameter]
    public bool ShowCellDataAsTooltip { get; set; }

    [Parameter]
    public bool ShowColumnTitleAsTooltip { get; set; }

    [Parameter]
    public EventCallback<DataGridColumnReorderingEventArgs<TItem>> ColumnReordering { get; set; }

    [Parameter]
    public EventCallback<DataGridColumnReorderedEventArgs<TItem>> ColumnReordered { get; set; }

    [Parameter]
    public EventCallback<DataGridColumnResizedEventArgs<TItem>> ColumnResized { get; set; }

    [Parameter]
    public bool AllowGrouping { get; set; }

    [Parameter]
    public EventCallback<DataGridColumnGroupEventArgs<TItem>> Group { get; set; }

    [Parameter]
    public string? GroupPanelText { get; set; }

    [Parameter]
    public bool HideGroupedColumn { get; set; }

    [Parameter]
    public bool GroupFootersAlwaysVisible { get; set; }

    [Parameter]
    public bool AllowAlternatingRows { get; set; }

    [Parameter]
    public DataGridGridLines GridLines { get; set; } = DataGridGridLines.Horizontal;

    [Parameter]
    public bool GreyHeader { get; set; }

    [Parameter]
    public bool NoBorder { get; set; }

    [Parameter]
    public bool WrapHeaders { get; set; }

    [Parameter]
    public bool WrapCells { get; set; }

    [Parameter]
    public bool Responsive { get; set; } = true;

    [Parameter]
    public bool AllowVirtualization { get; set; }

    [Parameter]
    public int VirtualizationOverscanCount { get; set; } = 3;

    [Parameter]
    public bool ShowHeader { get; set; } = true;

    [Parameter]
    public bool ShowEmptyMessage { get; set; } = true;

    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    [Parameter]
    public RenderFragment<TItem>? EditTemplate { get; set; }

    [Parameter]
    public DataGridEditMode EditMode { get; set; } = DataGridEditMode.Single;

    [Parameter]
    public string? KeyProperty { get; set; }

    [Parameter]
    public int FilterDelay { get; set; } = 300;

    [Parameter]
    public bool FilterAsYouType { get; set; }

    [Parameter]
    public string? FilterDateFormat { get; set; }

    [Parameter]
    public bool AllowFilterDateInput { get; set; } = true;

    [Parameter]
    public string? ColumnWidth { get; set; }

    [Parameter]
    public bool GotoFirstPageOnSort { get; set; } = true;

    [Parameter]
    public bool ShowExpandColumn { get; set; }

    [Parameter]
    public bool ShowExpandAll { get; set; }

    [Parameter]
    public bool ShowGroupExpandColumn { get; set; } = true;

    [Parameter]
    public bool? AllGroupsExpanded { get; set; }

    [Parameter]
    public EventCallback<bool?> AllGroupsExpandedChanged { get; set; }

    [Parameter]
    public DataGridExpandMode ExpandMode { get; set; } = DataGridExpandMode.Multiple;

    [Parameter]
    public bool AllowRowSelectOnRowClick { get; set; }

    [Parameter]
    public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.Single;

    [Parameter]
    public IList<TItem>? Value { get; set; }

    [Parameter]
    public EventCallback<IList<TItem>> ValueChanged { get; set; }

    [Parameter]
    public string ActionColumnWidth { get; set; } = "5rem";

    [Parameter]
    public RenderFragment<TItem>? Template { get; set; }

    [Parameter]
    public RenderFragment<TItem>? DetailTemplate { get; set; }

    [Parameter]
    public bool AllowRowReorder { get; set; }

    [Parameter]
    public Func<TItem, string>? RowKey { get; set; }

    [Parameter]
    public EventCallback<AdmiralDataGridRowReorderEventArgs<TItem>> RowReordered { get; set; }

    [Parameter]
    public string RowReorderAriaLabel { get; set; } = "Переместить строку";

    [Parameter]
    public Action<RowRenderEventArgs<TItem>>? RowRender { get; set; }

    [Parameter]
    public EventCallback<DataGridRowMouseEventArgs<TItem>> RowClick { get; set; }

    [Parameter]
    public EventCallback<TItem> RowSelect { get; set; }

    [Parameter]
    public EventCallback<TItem> RowDeselect { get; set; }

    [Parameter]
    public EventCallback<DataGridRowMouseEventArgs<TItem>> RowDoubleClick { get; set; }

    [Parameter]
    public EventCallback<TItem> RowExpand { get; set; }

    [Parameter]
    public EventCallback<TItem> RowCollapse { get; set; }

    [Parameter]
    public EventCallback<TItem> RowEdit { get; set; }

    [Parameter]
    public EventCallback<TItem> RowUpdate { get; set; }

    [Parameter]
    public EventCallback<TItem> RowCreate { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? GridClass { get; set; }

    [Parameter]
    public string? GridStyle { get; set; }

    [Parameter]
    public IReadOnlyDictionary<string, object>? GridAttributes { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private RadzenDataGrid<TItem>? grid;
    private ElementReference rootElement;
    private IReadOnlyList<AdmiralPaneColumn> paneColumns = [];
    private string columnSignature = string.Empty;
    private string searchValue = string.Empty;
    private string lastSearchParameter = string.Empty;
    private IReadOnlyDictionary<string, object>? RootAttributes { get; set; }
    private IReadOnlyDictionary<string, object>? EffectiveGridAttributes { get; set; }
    private static readonly HashSet<string> RadzenGridParameterNames = typeof(RadzenDataGrid<TItem>)
        .GetProperties()
        .Where(property => Attribute.IsDefined(property, typeof(ParameterAttribute), true))
        .Select(property => property.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    private int effectivePageSize = 10;
    private int lastPageSizeParameter;
    private int currentPage = 1;
    private int externalCount;
    private AdmiralDataGridLocalization Text => Localization ?? AdmiralDataGridLocalization.ForCurrentCulture();
    private bool HasPaging => AllowPaging && Pagination != AdmiralDataGridPagination.None;
    private bool UsesPaginationOne => Pagination is AdmiralDataGridPagination.PaginationOne or AdmiralDataGridPagination.PaginationOneSimple;
    private int EffectiveCount => LoadData.HasDelegate ? Count : externalCount;
    private RenderFragment<TItem>? EffectiveDetailTemplate => Template ?? DetailTemplate;
    private IEnumerable<TItem> FilteredItems => string.IsNullOrWhiteSpace(searchValue)
        ? Data
        : Data.Where(item => MatchesSearch(item, searchValue));

    protected override void OnParametersSet()
    {
        SplitAdditionalAttributes();
        if (!string.Equals(SearchValue, lastSearchParameter, StringComparison.Ordinal))
        {
            lastSearchParameter = SearchValue;
            searchValue = SearchValue;
        }
        if (PageSize != lastPageSizeParameter)
        {
            lastPageSizeParameter = PageSize;
            effectivePageSize = Math.Max(PageSize, 1);
        }
        var nextSignature = string.Join('|', Columns.Select(column => $"{column.Id}:{column.Title}:{column.Visible}:{column.DisabledInPicker}"));
        if (!string.Equals(columnSignature, nextSignature, StringComparison.Ordinal))
        {
            columnSignature = nextSignature;
            paneColumns = Columns.Select(column => new AdmiralPaneColumn(column.Id, column.Title, column.Visible, column.DisabledInPicker)).ToArray();
        }

        externalCount = LoadData.HasDelegate ? Count : FilteredItems.Count();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await ConnectRowReorderAsync();

        if (UsesPaginationOne && grid is not null && grid.Count != externalCount)
        {
            externalCount = grid.Count;
            await InvokeAsync(StateHasChanged);
        }
    }

    public Task Reload() => grid?.Reload() ?? Task.CompletedTask;
    public Task GoToPage(int page) => ChangeExternalPage(Math.Max(page, 1));

    private bool IsColumnVisible(string id) => paneColumns.FirstOrDefault(column => column.Id == id)?.Visible ?? true;

    private async Task HandleSearchChanged(string value)
    {
        searchValue = value;
        currentPage = 1;
        externalCount = FilteredItems.Count();
        await SearchValueChanged.InvokeAsync(value);
        if (grid is not null && HasPaging)
        {
            await grid.FirstPage(true);
        }
    }

    private async Task HandleColumnsChanged(IReadOnlyList<AdmiralPaneColumn> value)
    {
        paneColumns = value;
        await VisibleColumnsChanged.InvokeAsync(value);
        await InvokeAsync(StateHasChanged);
    }

    private async Task InvokeRowAction(AdmiralDataGridAction<TItem> action, TItem item)
    {
        if (action.IsDisabled?.Invoke(item) == true)
        {
            return;
        }

        action.Execute?.Invoke(item);

        if (action.ExecuteAsync is not null)
        {
            await action.ExecuteAsync(item);
        }

        await RowActionClicked.InvokeAsync(new(action.Id, item));
    }

    private string GetActionAriaLabel(AdmiralDataGridAction<TItem> action, TItem item) =>
        action.AriaLabelFor?.Invoke(item) ?? action.AriaLabel ?? action.Text ?? action.Id;

    private async Task HandleGridPage(PagerEventArgs args)
    {
        currentPage = args.PageIndex + 1;
        await Page.InvokeAsync(args);
        await InvokeAsync(StateHasChanged);
    }

    private void SplitAdditionalAttributes()
    {
        var root = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var gridParameters = GridAttributes is null
            ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(GridAttributes, StringComparer.OrdinalIgnoreCase);

        if (AdditionalAttributes is not null)
        {
            foreach (var attribute in AdditionalAttributes)
            {
                if (RadzenGridParameterNames.Contains(attribute.Key))
                {
                    gridParameters[attribute.Key] = attribute.Value;
                }
                else
                {
                    root[attribute.Key] = attribute.Value;
                }
            }
        }

        RootAttributes = root;
        EffectiveGridAttributes = gridParameters;
    }

    private Task HandleGridFilter(DataGridColumnFilterEventArgs<TItem> args)
    {
        currentPage = 1;
        _ = RefreshFilteredCountAsync();
        return Task.CompletedTask;
    }

    private async Task RefreshFilteredCountAsync()
    {
        await Task.Delay(25);
        if (grid is not null)
        {
            externalCount = grid.View.Count();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ChangeExternalPage(int page)
    {
        currentPage = page;
        if (grid is not null)
        {
            await grid.GoToPage(page - 1, true);
        }
    }

    private async Task ChangeExternalPageSize(int value)
    {
        effectivePageSize = Math.Max(value, 1);
        currentPage = 1;
        if (grid is not null)
        {
            await grid.FirstPage(true);
        }
    }

    private bool MatchesSearch(TItem item, string query)
    {
        if (SearchPredicate is not null)
        {
            return SearchPredicate(item, query);
        }

        return Columns.Where(column => column.Searchable)
            .Select(column => ReadProperty(item, column.Property)?.ToString())
            .Any(value => value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true);
    }

    private static object? ReadProperty(object? instance, string path)
    {
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (instance is null)
            {
                return null;
            }

            instance = instance.GetType().GetProperty(part)?.GetValue(instance);
        }

        return instance;
    }

    private ButtonSize RowActionButtonSize => Size == AdmiralDataGridSize.Small ? ButtonSize.ExtraSmall : ButtonSize.Small;
    private AdmiralGroupActionsPaneSize ToolbarSize => Size switch
    {
        AdmiralDataGridSize.Small => AdmiralGroupActionsPaneSize.Small,
        AdmiralDataGridSize.Medium => AdmiralGroupActionsPaneSize.Medium,
        AdmiralDataGridSize.Large => AdmiralGroupActionsPaneSize.Large,
        _ => AdmiralGroupActionsPaneSize.ExtraLarge
    };

    private string RootCssClass => string.Join(' ', new[] { "admiral-data-grid-shell", Class }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private string GridCssClass => string.Join(' ', new[]
    {
        "admiral-data-grid",
        Size switch
        {
            AdmiralDataGridSize.Small => "admiral-table--s admiral-pagination-two--s",
            AdmiralDataGridSize.Large => "admiral-table--l",
            AdmiralDataGridSize.ExtraLarge => "admiral-table--xl",
            _ => "admiral-table--m"
        },
        GreyHeader ? "admiral-table--grey-header" : null,
        NoBorder ? "admiral-table--no-border" : null,
        WrapHeaders ? "admiral-table--wrap-header" : null,
        WrapCells ? "admiral-table--wrap-cells" : null,
        UsesPaginationOne ? "admiral-data-grid--external-pager" : null,
        AllowRowReorder ? "admiral-data-grid--row-reorder" : null,
        GridClass
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

}
