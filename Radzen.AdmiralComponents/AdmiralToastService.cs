using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public sealed class AdmiralToastService : IDisposable
{
    private readonly List<AdmiralToastEntry> entries = [];
    private readonly Dictionary<Guid, CancellationTokenSource> timers = [];
    private readonly object sync = new();
    private AdmiralToastPosition position = AdmiralToastPosition.TopRight;
    private string width = "384px";
    public event Action? Changed;
    public IReadOnlyList<AdmiralToastEntry> Entries { get { lock (sync) return entries.ToArray(); } }
    public AdmiralToastPosition Position
    {
        get => position;
        set { if (position == value) return; position = value; Changed?.Invoke(); }
    }
    public string Width
    {
        get => width;
        set { if (width == value) return; width = value; Changed?.Invoke(); }
    }

    public Guid Show(AdmiralToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var id = Guid.NewGuid();
        CancellationTokenSource? cancellation = null;
        lock (sync)
        {
            entries.Insert(0, new AdmiralToastEntry(id, options));
            if (options.AutoDeleteTime is { } duration && duration > TimeSpan.Zero)
            {
                cancellation = new CancellationTokenSource();
                timers[id] = cancellation;
            }
        }
        Changed?.Invoke();
        if (cancellation is not null) _ = RemoveAfterAsync(id, options.AutoDeleteTime!.Value, cancellation.Token);
        return id;
    }

    public bool Remove(Guid id)
    {
        CancellationTokenSource? cancellation;
        bool removed;
        lock (sync)
        {
            timers.Remove(id, out cancellation);
            removed = entries.RemoveAll(entry => entry.Id == id) > 0;
        }
        if (cancellation is not null)
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
        if (removed) Changed?.Invoke();
        return removed;
    }

    public void Clear()
    {
        foreach (var id in Entries.Select(entry => entry.Id)) Remove(id);
    }

    private async Task RemoveAfterAsync(Guid id, TimeSpan duration, CancellationToken cancellation)
    {
        try
        {
            await Task.Delay(duration, cancellation);
            Remove(id);
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        lock (sync)
        {
            foreach (var cancellation in timers.Values)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }
            timers.Clear();
            entries.Clear();
        }
    }
}

public sealed record AdmiralToastEntry(Guid Id, AdmiralToastOptions Options);
