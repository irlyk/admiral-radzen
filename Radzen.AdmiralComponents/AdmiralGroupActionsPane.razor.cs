using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Admiral.Components.Models;
using Radzen.Blazor;

namespace Radzen.Admiral.Components;

public partial class AdmiralGroupActionsPane
{

    [Parameter]
    public IReadOnlyList<AdmiralActionItem> Actions { get; set; } = [];

    [Parameter]
    public EventCallback<AdmiralActionItem> ActionClicked { get; set; }

    [Parameter]
    public AdmiralGroupActionsPaneSize Size { get; set; } = AdmiralGroupActionsPaneSize.ExtraLarge;

    [Parameter]
    public IReadOnlyList<AdmiralPaneColumn> Columns { get; set; } = [];

    [Parameter]
    public EventCallback<IReadOnlyList<AdmiralPaneColumn>> ColumnsChanged { get; set; }

    [Parameter]
    public string SearchValue { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchValueChanged { get; set; }

    [Parameter]
    public bool ShowSettings { get; set; } = true;

    [Parameter]
    public EventCallback SettingsClicked { get; set; }

    [Parameter]
    public string SearchPlaceholder { get; set; } = "Search";

    [Parameter]
    public string SearchAriaLabel { get; set; } = "Search";

    [Parameter]
    public string OpenSearchAriaLabel { get; set; } = "Open search";

    [Parameter]
    public string CloseSearchAriaLabel { get; set; } = "Close search";

    [Parameter]
    public string ConfigureColumnsAriaLabel { get; set; } = "Configure columns";

    [Parameter]
    public string VisibleColumnsAriaLabel { get; set; } = "Visible columns";

    [Parameter]
    public string SettingsAriaLabel { get; set; } = "Settings";

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private bool searchOpen;
    private bool columnsOpen;
    private ElementReference columnsTrigger;
    private ElementReference searchInput;
    private bool focusSearch;
    private bool focusSearchTrigger;
    private RadzenButton? searchTrigger;
    private RadzenPopup? columnsPopup;
    private bool IsCompact => Size is AdmiralGroupActionsPaneSize.Small or AdmiralGroupActionsPaneSize.Medium;
    private ButtonSize ActionButtonSize => IsCompact ? ButtonSize.ExtraSmall : ButtonSize.Small;
    private ButtonSize ToolButtonSize => IsCompact ? ButtonSize.ExtraSmall : ButtonSize.Medium;
    private string CssClass => string.Join(' ', new[] { "admiral-group-actions-pane", Class }
        .Prepend(Size switch
        {
            AdmiralGroupActionsPaneSize.Small => "admiral-group-actions-pane--s",
            AdmiralGroupActionsPaneSize.Medium => "admiral-group-actions-pane--m",
            AdmiralGroupActionsPaneSize.Large => "admiral-group-actions-pane--l",
            _ => "admiral-group-actions-pane--xl"
        })
        .Where(value => !string.IsNullOrWhiteSpace(value)));

    private Task InvokeAction(AdmiralActionItem item) => item.Disabled
        ? Task.CompletedTask
        : ActionClicked.InvokeAsync(item);

    private void OpenSearch(MouseEventArgs _)
    {
        searchOpen = true;
        focusSearch = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (focusSearch)
        {
            focusSearch = false;
            await searchInput.FocusAsync();
        }
        else if (focusSearchTrigger && searchTrigger is not null)
        {
            focusSearchTrigger = false;
            await searchTrigger.Element.FocusAsync();
        }
    }

    private Task SearchKeyDown(KeyboardEventArgs args) => args.Key == "Escape" ? CloseSearch() : Task.CompletedTask;

    private async Task CloseSearch()
    {
        searchOpen = false;
        focusSearchTrigger = true;
        await SearchValueChanged.InvokeAsync(string.Empty);
    }

    private Task HandleSearchInput(ChangeEventArgs args) =>
        SearchValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task ChangeColumn(AdmiralPaneColumn column, bool visible)
    {
        var updated = Columns.Select(item => item.Id == column.Id ? item with { Visible = visible } : item).ToArray();
        return ColumnsChanged.InvokeAsync(updated);
    }

    private Task ToggleColumns(MouseEventArgs _) => columnsPopup?.ToggleAsync(columnsTrigger, syncWidth: true) ?? Task.CompletedTask;
    private void OnColumnsOpened() => columnsOpen = true;
    private void OnColumnsClosed() => columnsOpen = false;
}
