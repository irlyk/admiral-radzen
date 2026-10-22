using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components.Internal;

// Tree traversal and cascade rules shared by the input and its recursive branches.
internal static class AdmiralTreeSelectItems
{
    internal static IEnumerable<AdmiralTreeSelectItem> SelectedNodes(
        IEnumerable<AdmiralTreeSelectItem> items,
        HashSet<string> selected,
        AdmiralTreeSelectCheckedStrategy strategy,
        bool selectedAncestor = false)
    {
        foreach (var item in items)
        {
            var isSelected = selected.Contains(item.Id);
            var show = strategy switch
            {
                AdmiralTreeSelectCheckedStrategy.All => isSelected,
                AdmiralTreeSelectCheckedStrategy.Child => isSelected && item.Children.Count == 0,
                _ => isSelected && !selectedAncestor
            };
            if (show)
            {
                yield return item;
            }
            foreach (var child in SelectedNodes(item.Children, selected, strategy, selectedAncestor || isSelected))
            {
                yield return child;
            }
        }
    }

    internal static bool MatchesQuery(AdmiralTreeSelectItem item, string query) =>
        string.IsNullOrWhiteSpace(query)
        || item.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase)
        || item.Children.Any(child => MatchesQuery(child, query));

    internal static AdmiralTreeSelectItem? FindParent(IEnumerable<AdmiralTreeSelectItem> items, string id)
    {
        foreach (var item in items)
        {
            if (item.Children.Any(child => child.Id == id))
            {
                return item;
            }
            if (FindParent(item.Children, id) is { } parent)
            {
                return parent;
            }
        }
        return null;
    }
    internal static void NormalizeParents(IEnumerable<AdmiralTreeSelectItem> items, HashSet<string> selected)
    {
        foreach (var item in items.Where(item => !item.Disabled && !item.ReadOnly))
        {
            NormalizeParents(item.Children, selected);
            var children = item.Children.Where(child => !child.Disabled && !child.ReadOnly).ToArray();
            if (children.Length == 0)
            {
                continue;
            }
            if (children.All(child => selected.Contains(child.Id)))
            {
                selected.Add(item.Id);
            }
            else
            {
                selected.Remove(item.Id);
            }
        }
    }

    internal static IEnumerable<AdmiralTreeSelectItem> SelectableDescendants(AdmiralTreeSelectItem item)
    {
        if (item.Disabled || item.ReadOnly)
        {
            yield break;
        }
        yield return item;
        foreach (var child in item.Children.SelectMany(SelectableDescendants))
        {
            yield return child;
        }
    }

    internal static AdmiralTreeSelectItem? Find(IEnumerable<AdmiralTreeSelectItem> items, string? id)
    {
        if (id is null)
        {
            return null;
        }
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item;
            }
            var child = Find(item.Children, id);
            if (child is not null)
            {
                return child;
            }
        }
        return null;
    }

}
