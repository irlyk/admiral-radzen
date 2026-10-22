namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralAvatarItem(
    string Name,
    string? Initials = null,
    string? ImageUrl = null,
    string? Icon = null,
    AdmiralAvatarAppearance? Appearance = null,
    AdmiralAvatarStatus Status = AdmiralAvatarStatus.None,
    bool ShowActivityRing = false,
    bool Disabled = false);
