using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Internal;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralImageViewer : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyList<AdmiralImageViewerItem> Items { get; set; } = [];

    [Parameter]
    public AdmiralImageViewerDimension Dimension { get; set; } = AdmiralImageViewerDimension.M;

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public int ActiveIndex { get; set; }

    [Parameter]
    public EventCallback<int> ActiveIndexChanged { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public bool ShowCounter { get; set; } = true;

    [Parameter]
    public bool ShowNavigation { get; set; } = true;

    [Parameter]
    public double MinScale { get; set; } = 0.5;

    [Parameter]
    public double MaxScale { get; set; } = 3;

    [Parameter]
    public double ScaleStep { get; set; } = 0.25;

    [Parameter]
    public string DialogAriaLabel { get; set; } = "Просмотр изображения";

    [Parameter]
    public string ToolbarAriaLabel { get; set; } = "Действия с изображением";

    [Parameter]
    public string OpenText { get; set; } = "Открыть";

    [Parameter]
    public string CloseText { get; set; } = "Закрыть";

    [Parameter]
    public string LoadingText { get; set; } = "Загрузка изображения…";

    [Parameter]
    public string ErrorText { get; set; } = "Не удалось загрузить изображение";

    [Parameter]
    public string EmptyText { get; set; } = "Нет изображений";

    [Parameter]
    public string FlipHorizontalText { get; set; } = "Отразить по горизонтали";

    [Parameter]
    public string FlipVerticalText { get; set; } = "Отразить по вертикали";

    [Parameter]
    public string RotateLeftText { get; set; } = "Повернуть влево";

    [Parameter]
    public string RotateRightText { get; set; } = "Повернуть вправо";

    [Parameter]
    public string ZoomOutText { get; set; } = "Уменьшить";

    [Parameter]
    public string ZoomInText { get; set; } = "Увеличить";

    [Parameter]
    public string PreviousText { get; set; } = "Предыдущее изображение";

    [Parameter]
    public string NextText { get; set; } = "Следующее изображение";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private ElementReference dialog;
    private ElementReference surface;
    private IJSObjectReference? interactionModule;
    private IJSObjectReference? interaction;
    private DotNetObjectReference<AdmiralImageViewer>? selfReference;
    private string? previousImage;
    private int resetVersion;
    private bool wasVisible;
    // Image load can trigger another render while a JS import is still pending.
    private readonly SemaphoreSlim renderGate = new(1, 1);
    private bool disposed;
    private bool internalVisible;
    private bool imageLoading;
    private bool imageFailed;
    private AdmiralFocusScope? focusScope;
    private int internalIndex;
    private int rotation;
    private double scale = 1;
    private int flipX = 1;
    private int flipY = 1;
    private bool ActionsDisabled => imageFailed || imageLoading;
    private string MiniatureIconStyle
    {
        get
        {
            var size = Dimension is AdmiralImageViewerDimension.Xxs or AdmiralImageViewerDimension.Xs ? 20
                : Dimension is AdmiralImageViewerDimension.L or AdmiralImageViewerDimension.Xl ? 40 : 24;
            return $"width:{size}px;height:{size}px;font-size:{size}px;line-height:{size}px";
        }
    }
    private bool EffectiveVisible => VisibleChanged.HasDelegate ? Visible : internalVisible;
    private int SafeActiveIndex => Items.Count == 0 ? 0 : Math.Clamp(ActiveIndexChanged.HasDelegate ? ActiveIndex : internalIndex, 0, Items.Count - 1);
    private string CssClass => $"admiral-image-viewer admiral-image-viewer--{Dimension.ToString().ToLowerInvariant()} {Class}";
    private string ImageIdentity => Items.Count == 0 ? string.Empty : $"{SafeActiveIndex}:{Items[SafeActiveIndex].Src}";
    private string TransformStyle => $"transform: translate(var(--admiral-image-pan-x, 0px), var(--admiral-image-pan-y, 0px)) rotate({rotation}deg) scale({scale.ToString(CultureInfo.InvariantCulture)}) scaleX({flipX}) scaleY({flipY});";

    protected override void OnParametersSet()
    {
        if (!double.IsFinite(MinScale) || !double.IsFinite(MaxScale) || !double.IsFinite(ScaleStep)
            || MinScale <= 0 || MaxScale < MinScale || ScaleStep <= 0)
        {
            throw new ArgumentException("Scale limits and step must be finite and positive; MaxScale must not be less than MinScale.");
        }
        if (previousImage != ImageIdentity || (EffectiveVisible && !wasVisible))
        {
            previousImage = ImageIdentity;
            ResetTransform();
            imageLoading = true;
            imageFailed = false;
        }
        wasVisible = EffectiveVisible;
        scale = Math.Clamp(scale, MinScale, MaxScale);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await renderGate.WaitAsync();
        try
        {
            if (disposed)
            {
                return;
            }
            await UpdateBrowserAsync();
        }
        finally
        {
            renderGate.Release();
        }
    }

    private async Task UpdateBrowserAsync()
    {
        focusScope ??= new(JS);
        await focusScope.UpdateAsync(dialog, EffectiveVisible && Items.Count > 0);
        if (EffectiveVisible && Items.Count > 0)
        {
            interactionModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-image-viewer.js");
            if (!EffectiveVisible || Items.Count == 0 || disposed)
            {
                return;
            }
            selfReference ??= DotNetObjectReference.Create(this);
            interaction ??= await interactionModule.InvokeAsync<IJSObjectReference>("connect", dialog, surface, selfReference);
            await interaction.InvokeVoidAsync("update", ImageIdentity, resetVersion, scale, rotation);
        }
        else if (interaction is not null)
        {
            await interaction.InvokeVoidAsync("dispose");
            await interaction.DisposeAsync();
            interaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        await renderGate.WaitAsync();
        try
        {
            await DisposeBrowserAsync();
        }
        finally
        {
            renderGate.Release();
            selfReference?.Dispose();
        }
    }

    private async Task DisposeBrowserAsync()
    {
        if (focusScope is not null)
        {
            await focusScope.DisposeAsync();
        }
        try
        {
            if (interaction is not null)
            {
                await interaction.InvokeVoidAsync("dispose");
                await interaction.DisposeAsync();
            }
            if (interactionModule is not null)
            {
                await interactionModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
    }

    private async Task Open(int index)
    {
        ResetTransform();
        await SetActive(index);
        imageLoading = true;
        imageFailed = false;
        await SetVisible(true);
    }

    private Task Close() => SetVisible(false);
    private async Task SetVisible(bool value)
    {
        if (!VisibleChanged.HasDelegate)
        {
            internalVisible = value;
            wasVisible = value;
        }
        await VisibleChanged.InvokeAsync(value);
    }

    private async Task SetActive(int index)
    {
        if (index < 0 || index >= Items.Count || index == SafeActiveIndex)
        {
            return;
        }
        if (!ActiveIndexChanged.HasDelegate)
        {
            internalIndex = index;
            previousImage = ImageIdentity;
        }
        ResetTransform();
        imageLoading = true;
        imageFailed = false;
        await ActiveIndexChanged.InvokeAsync(index);
    }

    private Task Previous() => SetActive(SafeActiveIndex - 1);
    private Task Next() => SetActive(SafeActiveIndex + 1);
    private Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.CtrlKey || args.MetaKey || args.AltKey)
        {
            return Task.CompletedTask;
        }
        return args.Key switch
        {
            "Escape" => Close(),
            "ArrowLeft" => Previous(),
            "ArrowRight" => Next(),
            "ArrowUp" => ChangeScale(true),
            "ArrowDown" => ChangeScale(false),
            _ => Task.CompletedTask
        };
    }
    private Task ChangeScale(bool increase)
    {
        if (!imageFailed && !imageLoading)
        {
            if (increase) { ZoomIn(); }
            else { ZoomOut(); }
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task<double> ZoomImage(double factor, bool toggle = false)
    {
        await InvokeAsync(() =>
        {
            if (disposed || !EffectiveVisible || imageLoading || imageFailed || !double.IsFinite(factor) || factor <= 0)
            {
                return;
            }
            var target = Math.Clamp(factor, MinScale, MaxScale);
            if (toggle)
            {
                scale = Math.Abs(scale - target) < 0.01 ? Math.Clamp(1, MinScale, MaxScale) : target;
            }
            else
            {
                scale = Math.Clamp(scale * factor, MinScale, MaxScale);
            }
            StateHasChanged();
        });
        return scale;
    }

    [JSInvokable]
    public Task NavigateImage(int direction) => InvokeAsync(async () =>
    {
        if (disposed || !EffectiveVisible || direction is not (-1 or 1))
        {
            return;
        }
        await SetActive(SafeActiveIndex + direction);
        StateHasChanged();
    });
    private void ResetTransform()
    {
        scale = Math.Clamp(1, MinScale, MaxScale);
        resetVersion++;
        rotation = 0;
        flipX = flipY = 1;
    }
    private void ZoomIn() => scale = Math.Min(MaxScale, scale + Math.Max(0.01, ScaleStep));
    private void ZoomOut() => scale = Math.Max(MinScale, scale - Math.Max(0.01, ScaleStep));
    private void RotateLeft() => rotation -= 90;
    private void RotateRight() => rotation += 90;
    private void FlipHorizontal() => flipX *= -1;
    private void FlipVertical() => flipY *= -1;
    private void PreviewError()
    {
        imageFailed = true;
        imageLoading = false;
    }
    private void PreviewLoaded() => imageLoading = false;
}
