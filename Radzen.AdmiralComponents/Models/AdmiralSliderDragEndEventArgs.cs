namespace Radzen.Admiral.Components.Models;

/// <summary>A completed pointer interaction, after its final value-change callback.</summary>
public sealed record AdmiralSliderDragEndEventArgs(decimal Value, IReadOnlyList<decimal> Values, bool Cancelled);
