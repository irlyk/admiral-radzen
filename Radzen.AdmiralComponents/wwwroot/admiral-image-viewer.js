// Movement stays client-side; only scale and navigation cross the Blazor circuit.
export function connect(dialog, surface, dotnet) {
    if (!dialog.open) {
        dialog.showModal();
        dialog.querySelector('.admiral-image-viewer__close')?.focus({ preventScroll: true });
    }
    let disposed = false, identity, version, scale = 1, rotation = 0;
    let x = 0, y = 0, zoomPending = false, navigationPending = false, timer;
    let queued = emptyZoom(), activeZoom, swipe;
    const pointers = new Map();
    const image = () => surface.querySelector('.admiral-image-viewer__preview');
    function emptyZoom() { return { factor: 1, tx: 0, ty: 0, driftX: 0, driftY: 0 }; }
    const hasZoom = () => queued.factor !== 1 || queued.tx !== 0 || queued.ty !== 0;
    function local(point) {
        const bounds = surface.getBoundingClientRect();
        return { x: point.x - bounds.x - bounds.width / 2, y: point.y - bounds.y - bounds.height / 2 };
    }
    function pair() {
        const [a, b] = [...pointers.values()];
        return a && b ? { distance: Math.hypot(a.x - b.x, a.y - b.y), center: { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 } } : null;
    }
    function position() {
        const img = image();
        if (!img || disposed) return;
        const sideways = Math.abs(rotation % 180) === 90;
        const width = (sideways ? img.offsetHeight : img.offsetWidth) * scale;
        const height = (sideways ? img.offsetWidth : img.offsetHeight) * scale;
        const limitX = Math.max(0, (width - surface.clientWidth) / 2);
        const limitY = Math.max(0, (height - surface.clientHeight) / 2);
        x = Math.max(-limitX, Math.min(limitX, x));
        y = Math.max(-limitY, Math.min(limitY, y));
        surface.style.setProperty('--admiral-image-pan-x', `${x}px`);
        surface.style.setProperty('--admiral-image-pan-y', `${y}px`);
        surface.dataset.pannable = String(limitX > 0 || limitY > 0);
    }
    function applyZoom(operation, nextScale) {
        if (activeZoom !== operation || disposed || operation.identity !== identity || operation.version !== version) return;
        const ratio = nextScale / operation.fromScale;
        // Use the actual clamped scale. Keep the anchor fixed unless an image edge prevents it.
        const correction = Math.abs(operation.factor - 1) > 1e-8 ? (ratio - 1) / (operation.factor - 1) : 1;
        x = ratio * x + (operation.tx - operation.driftX) * correction + operation.driftX;
        y = ratio * y + (operation.ty - operation.driftY) * correction + operation.driftY;
        scale = nextScale;
        activeZoom = undefined;
        position();
    }
    function scheduleZoom() {
        if (!disposed && !zoomPending && !timer && hasZoom()) timer = setTimeout(flushZoom, 20);
    }
    async function requestZoom(operation, value, toggle) {
        zoomPending = true;
        activeZoom = { ...operation, fromScale: scale, identity, version };
        const current = activeZoom;
        try {
            const actualScale = await dotnet.invokeMethodAsync('ZoomImage', value, toggle);
            // The render may precede or follow the interop response; apply the operation once.
            applyZoom(current, actualScale);
        } catch { dispose(); }
        finally { zoomPending = false; scheduleZoom(); }
    }
    function flushZoom() {
        timer = undefined;
        if (disposed || zoomPending || !hasZoom()) return;
        const operation = queued;
        queued = emptyZoom();
        requestZoom(operation, operation.factor, false);
    }
    function zoom(ratio, fromPoint, toPoint = fromPoint) {
        if (!Number.isFinite(ratio) || ratio <= 0) return;
        const from = local(fromPoint), to = local(toPoint);
        queued.tx = ratio * queued.tx + to.x - ratio * from.x;
        queued.ty = ratio * queued.ty + to.y - ratio * from.y;
        queued.driftX = ratio * queued.driftX + to.x - from.x;
        queued.driftY = ratio * queued.driftY + to.y - from.y;
        queued.factor *= ratio;
        scheduleZoom();
    }
    const wheel = event => {
        if (event.target !== image()) return;
        event.preventDefault();
        const delta = event.deltaY * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? surface.clientHeight : 1);
        zoom(Math.exp(Math.max(-0.3, Math.min(0.3, -delta * 0.002))), { x: event.clientX, y: event.clientY });
    };
    async function navigate(direction) {
        if (disposed || navigationPending) return;
        navigationPending = true;
        try { await dotnet.invokeMethodAsync('NavigateImage', direction); }
        catch { dispose(); }
        finally { navigationPending = false; }
    }
    const down = event => {
        if (event.target !== image() || (event.pointerType === 'mouse' && event.button !== 0)) return;
        event.preventDefault();
        dialog.focus({ preventScroll: true });
        if (pointers.size === 0) {
            swipe = { id: event.pointerId, x: event.clientX, y: event.clientY, time: performance.now(), eligible: event.pointerType === 'touch' };
        } else if (swipe) {
            swipe.eligible = false;
        }
        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        surface.dataset.dragging = 'true';
        try { surface.setPointerCapture(event.pointerId); } catch { /* Synthetic events have no active pointer. */ }
    };
    const move = event => {
        const previous = pointers.get(event.pointerId);
        if (!previous) return;
        const oldPair = pair();
        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        if (pointers.size === 1) {
            x += event.clientX - previous.x;
            y += event.clientY - previous.y;
            position();
        } else if (oldPair?.distance > 0) {
            const nextPair = pair();
            zoom(nextPair.distance / oldPair.distance, oldPair.center, nextPair.center);
        }
    };
    const up = event => {
        const end = pointers.get(event.pointerId);
        if (!end) return;
        const gesture = swipe;
        pointers.delete(event.pointerId);
        if (pointers.size === 0) swipe = undefined;
        if (surface.hasPointerCapture(event.pointerId)) surface.releasePointerCapture(event.pointerId);
        surface.dataset.dragging = String(pointers.size > 0);
        position();
        if (event.type === 'pointerup' && gesture?.eligible && gesture.id === event.pointerId) {
            const dx = end.x - gesture.x, dy = end.y - gesture.y;
            if (Math.abs(dx) > 20 && Math.abs(dx) > Math.abs(dy) && performance.now() - gesture.time < 500) navigate(dx < 0 ? 1 : -1);
        }
    };
    function toggle(fit, point) {
        const img = image();
        if (!img || !img.offsetWidth || !img.offsetHeight || zoomPending) return;
        const sideways = Math.abs(rotation % 180) === 90;
        const target = fit ? Math.min(surface.clientWidth / (sideways ? img.offsetHeight : img.offsetWidth), surface.clientHeight / (sideways ? img.offsetWidth : img.offsetHeight))
            : img.naturalWidth / img.offsetWidth;
        clearTimeout(timer);
        timer = undefined;
        queued = emptyZoom();
        const anchor = point ? local(point) : { x: 0, y: 0 };
        const ratio = Math.abs(scale - target) < 0.01 ? 1 / scale : target / scale;
        requestZoom({ factor: ratio, tx: anchor.x * (1 - ratio), ty: anchor.y * (1 - ratio), driftX: 0, driftY: 0 }, target, true);
    }
    const doubleClick = event => {
        if (event.target === image() || event.target === surface) toggle(false, { x: event.clientX, y: event.clientY });
    };
    const keydown = event => {
        if (event.ctrlKey || event.metaKey || event.altKey || event.isComposing) return;
        if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key)) event.preventDefault();
        if (event.repeat && ['KeyF', 'Enter', 'Space'].includes(event.code)) { event.preventDefault(); return; }
        if (event.code === 'KeyF') { event.preventDefault(); toggle(true); }
        else if (!event.target.closest('button, input, a, select, textarea')) {
            if (event.key === 'Enter') { event.preventDefault(); toggle(false); }
            else if (event.key === ' ') { event.preventDefault(); navigate(1); }
        }
    };
    const cancel = event => event.preventDefault();
    const preventScroll = event => event.preventDefault();
    surface.addEventListener('wheel', wheel, { passive: false });
    surface.addEventListener('pointerdown', down);
    surface.addEventListener('pointermove', move);
    surface.addEventListener('pointerup', up);
    surface.addEventListener('pointercancel', up);
    surface.addEventListener('lostpointercapture', up);
    surface.addEventListener('dblclick', doubleClick);
    dialog.addEventListener('keydown', keydown);
    dialog.addEventListener('cancel', cancel);
    dialog.addEventListener('wheel', preventScroll, { passive: false });
    window.addEventListener('resize', position);
    function dispose() {
        if (disposed) return;
        disposed = true;
        clearTimeout(timer);
        pointers.clear();
        surface.removeEventListener('wheel', wheel);
        surface.removeEventListener('pointerdown', down);
        surface.removeEventListener('pointermove', move);
        surface.removeEventListener('pointerup', up);
        surface.removeEventListener('pointercancel', up);
        surface.removeEventListener('lostpointercapture', up);
        surface.removeEventListener('dblclick', doubleClick);
        dialog.removeEventListener('keydown', keydown);
        dialog.removeEventListener('cancel', cancel);
        dialog.removeEventListener('wheel', preventScroll);
        if (dialog.open) dialog.close();
        window.removeEventListener('resize', position);
    }
    return {
        update(nextIdentity, nextVersion, nextScale, nextRotation) {
            if (identity !== nextIdentity || version !== nextVersion) {
                x = y = 0;
                queued = emptyZoom();
                activeZoom = undefined;
                swipe = undefined;
                pointers.clear();
                surface.dataset.dragging = 'false';
            }
            identity = nextIdentity;
            version = nextVersion;
            rotation = nextRotation;
            if (activeZoom && (nextScale !== activeZoom.fromScale || activeZoom.factor === 1)) applyZoom(activeZoom, nextScale);
            scale = nextScale;
            position();
            const active = document.activeElement;
            if (active === document.body || (dialog.contains(active) && active.matches(':disabled'))) dialog.focus({ preventScroll: true });
        },
        dispose
    };
}
