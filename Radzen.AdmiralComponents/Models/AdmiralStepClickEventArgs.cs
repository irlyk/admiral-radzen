namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralStepClickEventArgs(int Index, bool Active, bool Completed, bool Disabled);
