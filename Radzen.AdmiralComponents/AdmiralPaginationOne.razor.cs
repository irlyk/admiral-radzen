using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralPaginationOne
{

    [Parameter]
    public int Page { get; set; } = 1;

    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 10;

    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    [Parameter]
    public int TotalItems { get; set; }

    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 20, 50, 100];

    [Parameter]
    public AdmiralPaginationSize Size { get; set; } = AdmiralPaginationSize.Medium;

    [Parameter]
    public bool Simple { get; set; }

    [Parameter]
    public bool PageSelectDisabled { get; set; }

    [Parameter]
    public bool PageSizeSelectDisabled { get; set; }

    [Parameter]
    public string ItemsPerPageText { get; set; } = "Items per page";

    [Parameter]
    public string ItemRangeFormat { get; set; } = "{0}–{1} of {2}";

    [Parameter]
    public string PageRangeFormat { get; set; } = "of {0} pages";

    [Parameter]
    public string PreviousPageAriaLabel { get; set; } = "Previous page";

    [Parameter]
    public string NextPageAriaLabel { get; set; } = "Next page";

    [Parameter]
    public string PageSelectAriaLabelFormat { get; set; } = "Page {0} of {1}";

    [Parameter]
    public string PageSizeAriaLabel { get; set; } = "Items per page";

    [Parameter]
    public string AriaLabel { get; set; } = "Pagination";

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private int SafePageSize => Math.Max(PageSize, 1);
    private int TotalPages => Math.Max((int)Math.Ceiling(Math.Max(TotalItems, 0) / (double)SafePageSize), 1);
    private int EffectivePage => Math.Clamp(Page, 1, TotalPages);
    private int RangeStart => TotalItems == 0 ? 0 : Math.Min(SafePageSize * (EffectivePage - 1) + 1, TotalItems);
    private int RangeEnd => Math.Min(SafePageSize * EffectivePage, Math.Max(TotalItems, 0));
    private IEnumerable<int> PageOptions => Enumerable.Range(1, TotalPages);
    private string ItemRangeText => string.Format(ItemRangeFormat, RangeStart, RangeEnd, Math.Max(TotalItems, 0));
    private string PageRangeText => string.Format(PageRangeFormat, TotalPages);
    private string PageAriaLabel => string.Format(PageSelectAriaLabelFormat, EffectivePage, TotalPages);
    private ButtonSize ButtonDimension => Size == AdmiralPaginationSize.Small ? ButtonSize.ExtraSmall : ButtonSize.Small;
    private const string SelectPopupStyle = "max-height:200px;overflow-x:hidden;--admiral-pagination-menu:1";

    private Task PreviousPage() => ChangePageAsync(EffectivePage - 1);
    private Task NextPage() => ChangePageAsync(EffectivePage + 1);

    private async Task OnPageSelected(object value)
    {
        if (value is int page)
        {
            await ChangePageAsync(page);
        }
    }

    private async Task OnPageSizeSelected(object value)
    {
        if (value is not int pageSize || pageSize <= 0 || pageSize == PageSize)
        {
            return;
        }

        // Capture the current page before the consumer updates PageSize. The new
        // size can clamp EffectivePage to 1 while the bound Page still holds an
        // older value (for example, page 2 of 15 items when changing 10 to 20).
        var resetPage = EffectivePage != 1;
        await PageSizeChanged.InvokeAsync(pageSize);
        if (resetPage && Page != 1)
        {
            await PageChanged.InvokeAsync(1);
        }
    }

    private Task ChangePageAsync(int page)
    {
        var next = Math.Clamp(page, 1, TotalPages);
        return next == EffectivePage ? Task.CompletedTask : PageChanged.InvokeAsync(next);
    }

    private string CssClass => string.Join(' ', new[]
    {
        "admiral-pagination-one",
        Size == AdmiralPaginationSize.Small ? "admiral-pagination-one--s" : "admiral-pagination-one--m",
        Simple ? "admiral-pagination-one--simple" : null,
        Class
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
