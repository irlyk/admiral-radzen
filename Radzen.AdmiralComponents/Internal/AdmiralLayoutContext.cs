using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components.Internal;

internal sealed class AdmiralLayoutContext
{
    public required Func<Task> ToggleOverlayAsync { get; init; }
    public required Func<Task> CloseOverlayAsync { get; init; }
    public required Func<Task> ToggleDesktopAsync { get; init; }
    public required Func<bool> IsOverlayOpen { get; init; }
    public required Func<AdmiralSideMenuState> GetSideMenuState { get; init; }
    public required string SideMenuId { get; init; }
}
