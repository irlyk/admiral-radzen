using Microsoft.AspNetCore.Components;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralTreeSelectBranch
{

    [Parameter]
    public IReadOnlyList<AdmiralTreeSelectItem> Items { get; set; } = [];

    [Parameter]
    public IReadOnlySet<string> SelectedIds { get; set; } = new HashSet<string>();

    [Parameter]
    public IReadOnlySet<string> ExpandedIds { get; set; } = new HashSet<string>();

    [Parameter]
    public string Query { get; set; } = "";

    [Parameter]
    public int Depth { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public string? ActiveId { get; set; }

    [Parameter]
    public string IdPrefix { get; set; } = "";

    [Parameter]
    public bool InheritedDisabled { get; set; }

    [Parameter]
    public bool InheritedReadOnly { get; set; }

    [Parameter]
    public EventCallback<AdmiralTreeSelectItem> SelectItem { get; set; }

    [Parameter]
    public EventCallback<string> ToggleExpanded { get; set; }

    [Parameter]
    public string ExpandText { get; set; } = "Expand";

    [Parameter]
    public string CollapseText { get; set; } = "Collapse";

    private bool IsVisible(AdmiralTreeSelectItem item) => Internal.AdmiralTreeSelectItems.MatchesQuery(item, Query);
    private bool IsExpanded(AdmiralTreeSelectItem item) => ExpandedIds.Contains(item.Id);
    private bool IsChecked(AdmiralTreeSelectItem item) => SelectedIds.Contains(item.Id)
        || (item.Children.Any(child => !child.Disabled && !child.ReadOnly)
            && item.Children.Where(child => !child.Disabled && !child.ReadOnly).All(IsChecked));
    private bool IsPartial(AdmiralTreeSelectItem item) => !IsChecked(item)
        && item.Children.Any(child => IsChecked(child) || IsPartial(child));
}
