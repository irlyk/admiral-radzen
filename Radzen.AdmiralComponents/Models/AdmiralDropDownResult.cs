namespace Radzen.Admiral.Components.Models;

/// <summary>
/// Contains one page of dropdown items and the total number of matching items.
/// </summary>
public sealed record AdmiralDropDownResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount)
{
    public static AdmiralDropDownResult<TItem> FromItems(IEnumerable<TItem> items)
    {
        var materialized = items as IReadOnlyList<TItem> ?? items.ToArray();
        return new(materialized, materialized.Count);
    }
}
