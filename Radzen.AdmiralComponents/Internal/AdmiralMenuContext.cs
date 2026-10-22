namespace Radzen.Admiral.Components.Internal;

internal sealed class AdmiralMenuContext
{
    public required Func<Task> CloseAsync { get; init; }
}
