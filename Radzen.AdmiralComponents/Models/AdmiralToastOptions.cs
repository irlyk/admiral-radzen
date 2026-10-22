using Microsoft.AspNetCore.Components;

namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralToastOptions
{
    public string? Title { get; init; }
    public string? Message { get; init; }
    public AdmiralNotificationStatus Status { get; init; } = AdmiralNotificationStatus.Info;
    public bool DisplayStatusIcon { get; init; } = true;
    public bool ShowProgress { get; init; }
    public TimeSpan? AutoDeleteTime { get; init; }
    public RenderFragment? Actions { get; init; }
}
