namespace Radzen.Admiral.Components;

/// <summary>
/// Admiral-prefixed DataGrid column with the complete RadzenDataGridColumn API.
/// </summary>
/// <typeparam name="TItem">Grid row type.</typeparam>
public class AdmiralDataGridColumn<TItem> : Radzen.Blazor.RadzenDataGridColumn<TItem> where TItem : notnull
{
}
