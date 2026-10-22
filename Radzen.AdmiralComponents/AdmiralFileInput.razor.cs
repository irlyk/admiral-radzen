using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

/// <summary>Admiral file selection surface; upload transport remains application-owned.</summary>
public partial class AdmiralFileInput
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public AdmiralFileInputDimension Dimension { get; set; } = AdmiralFileInputDimension.Xl;
    [Parameter]
    public string Title { get; set; } = "Перетащите файлы сюда или нажмите для выбора";
    [Parameter]
    public string? ExtraText { get; set; }
    [Parameter]
    public string Accept { get; set; } = "";
    [Parameter]
    public bool Multiple { get; set; } = true;
    [Parameter]
    public bool Disabled { get; set; }
    [Parameter]
    public bool Skeleton { get; set; }
    [Parameter]
    public int MaxFiles { get; set; } = 10;
    [Parameter]
    public long? MaxFileSize { get; set; }

    /// <summary>Controlled selection. Use files from the latest FilesSelected callback; remove or reorder them, or clear the list. Foreign browser files cannot be injected into InputFile.</summary>
    [Parameter]
    public IReadOnlyList<IBrowserFile>? Files { get; set; }

    /// <summary>Raised on selection, removal and reset. Supports @bind-Files.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> FilesChanged { get; set; }

    /// <summary>Raised after a native selection. Invalid selection clears previous files and returns an empty list, because InputFile invalidates their streams.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> FilesSelected { get; set; }
    [Parameter]
    public EventCallback<IBrowserFile> FileRemoved { get; set; }
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Custom keyboard-accessible trigger. Invoke the supplied async callback to open the native picker. Disable the trigger when Disabled or Skeleton is true.</summary>
    [Parameter]
    public RenderFragment<Func<Task>>? TriggerTemplate { get; set; }

    [Parameter]
    public string TooManyFilesText { get; set; } = "Слишком много файлов";
    [Parameter]
    public string FileTooLargeText { get; set; } = "Файл превышает допустимый размер";
    [Parameter]
    public string FileTypeErrorText { get; set; } = "Недопустимый формат файла";
    [Parameter]
    public string RemoveFileAriaLabel { get; set; } = "Удалить файл";
    [Parameter]
    public string? Class { get; set; }
    [Parameter]
    public string? Style { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private readonly string id = $"admiral-file-input-{Guid.NewGuid():N}";
    private readonly List<IBrowserFile> selectedFiles = [];
    private IReadOnlyList<IBrowserFile> nativeSelection = [];
    private InputFile? input;
    private IJSObjectReference? module;
    private string? error;
    private int inputVersion;
    private bool syncRequested = true;
    private string HintId => $"{id}-hint";
    private string ErrorId => $"{id}-error";
    private string? DescriptionIds => string.Join(' ', new[]
    {
        string.IsNullOrWhiteSpace(ExtraText) ? null : HintId,
        error is null ? null : ErrorId
    }.Where(value => value is not null)) is { Length: > 0 } ids ? ids : null;
    private string CssClass => string.Join(' ', new[]
    {
        "admiral-file-input-control", $"admiral-file-input-control--{Dimension.ToString().ToLowerInvariant()}",
        Disabled ? "admiral-file-input-control--disabled" : null,
        Skeleton ? "admiral-file-input-control--skeleton" : null,
        TriggerTemplate is not null ? "admiral-file-input-control--custom" : null, Class
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    protected override void OnParametersSet()
    {
        if (Files is null || selectedFiles.SequenceEqual(Files)) return;
        if (Files.Any(file => !nativeSelection.Contains(file)))
        {
            throw new InvalidOperationException("Files must contain only browser files from this input's latest selection.");
        }
        selectedFiles.Clear();
        selectedFiles.AddRange(Files);
        error = null;
        syncRequested = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-file-input.js");
        if (syncRequested && input?.Element is { } element)
        {
            syncRequested = false;
            await module.InvokeVoidAsync("initialize", element);
            var selection = nativeSelection.ToList();
            await module.InvokeVoidAsync("sync", element, selectedFiles.Select(file => selection.IndexOf(file)).ToArray());
        }
    }

    private async Task HandleChange(InputFileChangeEventArgs args)
    {
        if (Disabled || Skeleton) return;
        var files = args.GetMultipleFiles(int.MaxValue);
        nativeSelection = files;
        error = null;
        if (files.Count > (Multiple ? Math.Max(1, MaxFiles) : 1)) error = TooManyFilesText;
        else if (MaxFileSize is { } max && files.Any(file => file.Size > max)) error = FileTooLargeText;
        else if (files.Any(file => !IsAccepted(file))) error = FileTypeErrorText;
        selectedFiles.Clear();
        if (error is null) selectedFiles.AddRange(files);
        syncRequested = true;
        await NotifyFilesChangedAsync();
        await FilesSelected.InvokeAsync(selectedFiles.ToArray());
    }

    private bool IsAccepted(IBrowserFile file) => string.IsNullOrWhiteSpace(Accept) ||
        Accept.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(type => type.StartsWith('.')
                ? file.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase)
                : type.EndsWith("/*", StringComparison.Ordinal)
                    ? file.ContentType.StartsWith(type[..^1], StringComparison.OrdinalIgnoreCase)
                    : string.Equals(file.ContentType, type, StringComparison.OrdinalIgnoreCase));

    /// <summary>Open the native picker from a user action. Disabled and skeleton inputs ignore the request.</summary>
    public async Task OpenAsync()
    {
        if (!Disabled && !Skeleton && module is not null && input?.Element is { } element)
            await module.InvokeVoidAsync("open", element);
    }

    /// <summary>Remove a selected file. Remaining file streams stay valid until the next native selection or reset.</summary>
    public async Task RemoveAsync(IBrowserFile file)
    {
        if (Disabled || Skeleton || !selectedFiles.Remove(file)) return;
        error = null;
        syncRequested = true;
        await NotifyFilesChangedAsync();
        await FileRemoved.InvokeAsync(file);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Clear the selection and validation error, and replace the native input. Previous streams become invalid.</summary>
    public async Task ResetAsync()
    {
        selectedFiles.Clear();
        nativeSelection = [];
        error = null;
        inputVersion++;
        syncRequested = true;
        await NotifyFilesChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task NotifyFilesChangedAsync()
    {
        Files = selectedFiles.ToArray();
        await FilesChanged.InvokeAsync(Files);
    }

    private static string FormatSize(long size) => size < 1024 ? $"{size} B"
        : size < 1024 * 1024 ? $"{size / 1024.0:0.#} KB" : $"{size / (1024.0 * 1024):0.#} MB";

    public async ValueTask DisposeAsync()
    {
        if (module is null) return;
        try { await module.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
