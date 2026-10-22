// Only browser geometry crosses interop; partitioning and actions stay in C#.
export function connect(element, dotnet) {
    let disposed = false, adaptive = false, frame, pending = false, lastWidth = -1;
    let focused;
    const rememberFocus = event => { focused = event.target; };
    element.addEventListener('focusin', rememberFocus);
    async function measure() {
        frame = undefined;
        if (disposed || !adaptive || pending) return;
        const width = element.getBoundingClientRect().width;
        if (Math.abs(width - lastWidth) < 0.5) return;
        lastWidth = width;
        pending = true;
        try { await dotnet.invokeMethodAsync('UpdateAvailableWidth', width); }
        catch { dispose(); }
        finally { pending = false; schedule(); }
    }
    function schedule() {
        if (!disposed && adaptive && !frame) frame = requestAnimationFrame(measure);
    }
    const resize = new ResizeObserver(schedule);
    resize.observe(element);
    function dispose() {
        disposed = true;
        cancelAnimationFrame(frame);
        resize.disconnect();
        element.removeEventListener('focusin', rememberFocus);
    }
    return {
        update(enabled) {
            adaptive = enabled;
            if (focused && !focused.isConnected) {
                focused = undefined;
                // Resize may move the focused action into the menu or remove its trigger.
                if (document.activeElement === document.body) {
                    const target = element.querySelector('.admiral-menu__trigger button:not(:disabled)')
                        || element.querySelector('button:not(:disabled)');
                    target?.focus({ preventScroll: true });
                }
            }
            schedule();
        },
        dispose
    };
}
