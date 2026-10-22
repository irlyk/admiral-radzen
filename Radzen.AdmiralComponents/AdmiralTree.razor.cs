using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralTree : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;


    /// <summary>Hierarchy with unique nonempty Id values. Defaults to an empty tree.</summary>
    [Parameter]
    public IReadOnlyList<AdmiralTreeItem> Items { get; set; } = [];

    /// <summary>Selected node Id. Defaults to null; use ValueChanged for controlled selection.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Called when a node is selected through click, Enter or Space.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Checked node Ids. Defaults to empty; bind with CheckedValuesChanged.</summary>
    [Parameter]
    public IReadOnlyCollection<string> CheckedValues { get; set; } = [];

    /// <summary>New checked Ids, including normalized parents in cascading mode.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> CheckedValuesChanged { get; set; }

    /// <summary>Expanded branch Ids. Defaults to empty; bind with ExpandedValuesChanged.</summary>
    [Parameter]
    public IReadOnlyCollection<string> ExpandedValues { get; set; } = [];

    /// <summary>Called after a user expands or collapses a branch.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ExpandedValuesChanged { get; set; }

    /// <summary>Shows checkboxes. Defaults to true, like React Admiral Tree.</summary>
    [Parameter]
    public bool AllowCheckBoxes { get; set; } = true;

    /// <summary>Disable parent/child check propagation. Defaults to false.</summary>
    [Parameter]
    public bool CheckStrictly { get; set; }

    /// <summary>Blocks selection, expansion and keyboard mutation. Defaults to false.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Medium (40px) or Small (32px). Defaults to Medium.</summary>
    [Parameter]
    public AdmiralTreeSize Size { get; set; }

    /// <summary>Label-only template; do not put interactive controls inside tree rows.</summary>
    [Parameter]
    public RenderFragment<AdmiralTreeItem>? ItemTemplate { get; set; }

    /// <summary>Accessible tree name. Defaults to Дерево.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Дерево";

    /// <summary>Localized expand action label.</summary>
    [Parameter]
    public string ExpandAriaLabel { get; set; } = "Раскрыть";

    /// <summary>Localized collapse action label.</summary>
    [Parameter]
    public string CollapseAriaLabel { get; set; } = "Свернуть";

    /// <summary>Empty-tree message. Defaults to Нет элементов.</summary>
    [Parameter]
    public string EmptyText { get; set; } = "Нет элементов";

    /// <summary>Additional root classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Root HTML attributes, including style.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private sealed record Node(AdmiralTreeItem Item, string? Parent, int Depth, int Position, int SetSize, bool Disabled);
    private readonly Dictionary<string, Node> nodes = [];
    private readonly List<Node> allNodes = [];
    private readonly List<Node> visibleNodes = [];
    private readonly Dictionary<string, bool?> checkStates = [];
    private HashSet<string> checkedIds = [], expandedIds = [], previousChecked = [], previousExpanded = [];
    private string? selectedId, previousValue, activeId, pendingFocus;
    private ElementReference element;
    private IJSObjectReference? module, interaction;
    private readonly SemaphoreSlim renderGate = new(1, 1);
    private bool disposed;
    private string? SelectedValue => ValueChanged.HasDelegate ? Value : selectedId;
    private AdmiralIconSize IconSize => Size == AdmiralTreeSize.Small ? AdmiralIconSize.Medium : AdmiralIconSize.Large;
    private string CssClass => $"admiral-tree-view {(Size == AdmiralTreeSize.Small ? "admiral-tree-view--s" : null)} {Class}";

    protected override void OnParametersSet()
    {
        if (ValueChanged.HasDelegate || Value != previousValue)
        {
            selectedId = Value;
        }
        previousValue = Value;
        var incomingChecked = CheckedValues.ToHashSet();
        if (CheckedValuesChanged.HasDelegate || !previousChecked.SetEquals(incomingChecked))
        {
            checkedIds = new(incomingChecked);
        }
        previousChecked = incomingChecked;
        var incomingExpanded = ExpandedValues.ToHashSet();
        if (ExpandedValuesChanged.HasDelegate || !previousExpanded.SetEquals(incomingExpanded))
        {
            expandedIds = new(incomingExpanded);
        }
        previousExpanded = incomingExpanded;
        Rebuild();
    }

    private void Rebuild()
    {
        nodes.Clear();
        allNodes.Clear();
        visibleNodes.Clear();
        checkStates.Clear();
        Visit(Items, null, 0, Disabled, true);
        foreach (var node in allNodes.AsEnumerable().Reverse())
        {
            checkStates[node.Item.Id] = CalculateCheckState(node);
        }
        if (!visibleNodes.Any(node => node.Item.Id == activeId && !node.Disabled))
        {
            activeId = visibleNodes.FirstOrDefault(node => node.Item.Id == SelectedValue && !node.Disabled)?.Item.Id
                ?? visibleNodes.FirstOrDefault(node => !node.Disabled)?.Item.Id;
        }
    }

    private void Visit(IReadOnlyList<AdmiralTreeItem> items, string? parent, int depth, bool inheritedDisabled, bool visible)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var node = new Node(item, parent, depth, index + 1, items.Count, inheritedDisabled || item.Disabled);
            if (string.IsNullOrWhiteSpace(item.Id) || !nodes.TryAdd(item.Id, node))
            {
                throw new ArgumentException("Tree item Id values must be nonempty and unique.", nameof(Items));
            }
            allNodes.Add(node);
            if (visible) visibleNodes.Add(node);
            Visit(item.Children, item.Id, depth + 1, node.Disabled, visible && expandedIds.Contains(item.Id));
        }
    }

    private bool? CheckState(string id) => checkStates.GetValueOrDefault(id, false);

    private bool? CalculateCheckState(Node node)
    {
        if (CheckStrictly || node.Disabled)
        {
            return checkedIds.Contains(node.Item.Id);
        }
        var children = node.Item.Children.Where(child => !nodes[child.Id].Disabled).ToArray();
        if (children.Length == 0)
        {
            return checkedIds.Contains(node.Item.Id);
        }
        if (children.All(child => CheckState(child.Id) == true))
        {
            return true;
        }
        return children.Any(child => CheckState(child.Id) != false) ? null : false;
    }

    private async Task SelectNode(string id)
    {
        if (!nodes.TryGetValue(id, out var node) || node.Disabled)
        {
            return;
        }
        activeId = pendingFocus = id;
        selectedId = id;
        if (AllowCheckBoxes)
        {
            var value = CheckState(id) != true;
            SetChecked(node, value);
            Rebuild();
            if (!CheckStrictly)
            {
                foreach (var entry in nodes.Values.Where(entry => !entry.Disabled && entry.Item.Children.Count > 0))
                {
                    if (CheckState(entry.Item.Id) == true) checkedIds.Add(entry.Item.Id);
                    else checkedIds.Remove(entry.Item.Id);
                }
            }
            await CheckedValuesChanged.InvokeAsync(nodes.Keys.Where(checkedIds.Contains).ToArray());
        }
        await ValueChanged.InvokeAsync(id);
    }

    private void SetChecked(Node node, bool value)
    {
        if (node.Disabled)
        {
            return;
        }
        if (value)
        {
            checkedIds.Add(node.Item.Id);
        }
        else
        {
            checkedIds.Remove(node.Item.Id);
        }
        if (!CheckStrictly)
        {
            foreach (var child in node.Item.Children) SetChecked(nodes[child.Id], value);
        }
    }

    private async Task ToggleExpanded(string id)
    {
        if (!nodes.TryGetValue(id, out var node) || node.Disabled || node.Item.Children.Count == 0)
        {
            return;
        }
        if (!expandedIds.Add(id))
        {
            expandedIds.Remove(id);
        }
        activeId = pendingFocus = id;
        Rebuild();
        await ExpandedValuesChanged.InvokeAsync(nodes.Keys.Where(expandedIds.Contains).ToArray());
    }

    private async Task KeyDown(KeyboardEventArgs args)
    {
        if (Disabled || args.CtrlKey || args.MetaKey || args.AltKey || activeId is null) return;
        var enabled = visibleNodes.Where(node => !node.Disabled).ToArray();
        var index = Array.FindIndex(enabled, node => node.Item.Id == activeId);
        if (index < 0) return;
        var current = enabled[index];
        switch (args.Key)
        {
            case "ArrowDown": Focus(enabled[Math.Min(index + 1, enabled.Length - 1)].Item.Id); break;
            case "ArrowUp": Focus(enabled[Math.Max(index - 1, 0)].Item.Id); break;
            case "Home": Focus(enabled[0].Item.Id); break;
            case "End": Focus(enabled[^1].Item.Id); break;
            case "ArrowRight":
                if (current.Item.Children.Count > 0 && !expandedIds.Contains(activeId)) await ToggleExpanded(activeId);
                else if (current.Item.Children.Count > 0 && index + 1 < enabled.Length && enabled[index + 1].Depth > current.Depth) Focus(enabled[index + 1].Item.Id);
                break;
            case "ArrowLeft":
                if (current.Item.Children.Count > 0 && expandedIds.Contains(activeId)) await ToggleExpanded(activeId);
                else if (current.Parent is not null) Focus(current.Parent);
                break;
            case "Enter": case " ": await SelectNode(activeId); break;
            default:
                if (args.Key.Length == 1 && char.IsLetterOrDigit(args.Key[0]))
                {
                    var match = enabled.Skip(index + 1).Concat(enabled.Take(index + 1))
                        .FirstOrDefault(node => node.Item.Text.StartsWith(args.Key, StringComparison.CurrentCultureIgnoreCase));
                    if (match is not null) Focus(match.Item.Id);
                }
                break;
        }
    }

    private void Focus(string id) => activeId = pendingFocus = id;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await renderGate.WaitAsync();
        try
        {
            if (disposed) return;
            module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-tree.js");
            interaction ??= await module.InvokeAsync<IJSObjectReference>("connect", element);
            await interaction.InvokeVoidAsync("update", pendingFocus);
            pendingFocus = null;
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
            if (module is not null) await module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        finally { renderGate.Release(); }
    }
}
