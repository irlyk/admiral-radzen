const hints = new Map();
const margin = 16, gap = 8;

// Positioning is a browser concern. Visibility and callbacks stay in Blazor.
function position(state) {
    const popup = document.getElementById(state.id);
    if (!popup || !state.options.open) return;
    const viewport = window.visualViewport;
    const left = viewport?.offsetLeft ?? 0, top = viewport?.offsetTop ?? 0;
    const width = viewport?.width ?? innerWidth, height = viewport?.height ?? innerHeight;
    const target = state.anchor.getBoundingClientRect();
    const size = popup.getBoundingClientRect();
    const mobile = width < 640;
    const preferred = state.options.placement;
    const sides = mobile ? ['bottom', 'top']
        : preferred === 'auto' ? ['bottom', 'top', 'right', 'left']
        : [preferred, ...['bottom', 'top', 'right', 'left'].filter(side => side !== preferred)];
    const candidates = [];
    for (const side of sides) {
        const vertical = side === 'top' || side === 'bottom';
        const centered = vertical ? (target.left + target.right - size.width) / 2
            : (target.top + target.bottom - size.height) / 2;
        const alignments = mobile ? [left + (width - size.width) / 2]
            : [centered, vertical ? target.left : target.top,
                vertical ? target.right - size.width : target.bottom - size.height];
        for (const alignment of alignments) {
            candidates.push({ side,
                x: vertical ? alignment : side === 'left' ? target.left - size.width - gap : target.right + gap,
                y: !vertical ? alignment : side === 'top' ? target.top - size.height - gap : target.bottom + gap });
        }
    }
    const fits = value => value.x >= left + margin && value.y >= top + margin &&
        value.x + size.width <= left + width - margin && value.y + size.height <= top + height - margin;
    const selected = candidates.find(fits) ?? candidates.reduce((best, value) => {
        const visible = candidate => Math.max(0, Math.min(candidate.x + size.width, left + width - margin) - Math.max(candidate.x, left + margin)) *
            Math.max(0, Math.min(candidate.y + size.height, top + height - margin) - Math.max(candidate.y, top + margin));
        return visible(value) > visible(best) ? value : best;
    });
    popup.style.left = `${Math.max(left + margin, Math.min(selected.x, left + width - size.width - margin))}px`;
    popup.style.top = `${Math.max(top + margin, Math.min(selected.y, top + height - size.height - margin))}px`;
    popup.dataset.placement = selected.side;
}

export function connect(id, anchor, dotnet) {
    disconnect(id);
    const state = { id, anchor, dotnet, options: {}, frame: 0, timer: 0, observer: null, suppressed: false };
    hints.set(id, state);
    const popup = () => document.getElementById(id);
    const inside = node => node instanceof Node && (anchor.contains(node) || popup()?.contains(node));
    const request = open => {
        clearTimeout(state.timer);
        if (!state.options.disabled && state.options.open !== open) dotnet.invokeMethodAsync('SetOpenAsync', open);
    };
    const schedule = event => {
        if (!state.options.open) return;
        const internalScroll = event?.target instanceof Node && popup()?.contains(event.target);
        if (state.options.hover && !internalScroll) {
            state.suppressed = true;
            request(false);
            return;
        }
        cancelAnimationFrame(state.frame);
        state.frame = requestAnimationFrame(() => position(state));
    };
    const click = () => { if (!state.options.hover) request(true); };
    const over = event => {
        if (state.options.hover && !state.suppressed && inside(event.target)) {
            clearTimeout(state.timer);
            if (!inside(event.relatedTarget)) request(true);
        }
    };
    const out = event => {
        if (state.options.hover && inside(event.target) && !inside(event.relatedTarget)) {
            state.suppressed = false;
            state.timer = setTimeout(() => {
                if (!inside(document.activeElement)) request(false);
            }, 100);
        }
    };
    const focus = event => {
        if (!state.options.hover) return;
        if (inside(event.target)) { state.suppressed = false; request(true); }
        else if (!anchor.matches(':hover') && !popup()?.matches(':hover')) request(false);
    };
    const outside = event => {
        if (!state.options.hover && state.options.closeOnDocumentClick && !inside(event.target)) request(false);
    };
    const key = event => {
        if (!event.defaultPrevented && state.options.open && event.key === 'Escape' && inside(event.target)) {
            event.preventDefault();
            event.stopPropagation();
            request(false);
        }
    };
    anchor.addEventListener('click', click);
    document.addEventListener('pointerover', over);
    document.addEventListener('pointerout', out);
    document.addEventListener('focusin', focus);
    document.addEventListener('pointerdown', outside, true);
    document.addEventListener('keydown', key);
    document.addEventListener('scroll', schedule, true);
    window.addEventListener('resize', schedule);
    window.visualViewport?.addEventListener('resize', schedule);
    state.cleanup = () => {
        anchor.removeEventListener('click', click);
        document.removeEventListener('pointerover', over);
        document.removeEventListener('pointerout', out);
        document.removeEventListener('focusin', focus);
        document.removeEventListener('pointerdown', outside, true);
        document.removeEventListener('keydown', key);
        document.removeEventListener('scroll', schedule, true);
        window.removeEventListener('resize', schedule);
        window.visualViewport?.removeEventListener('resize', schedule);
    };
    state.observe = () => {
        state.observer?.disconnect();
        state.observer = new ResizeObserver(() => {
            cancelAnimationFrame(state.frame);
            state.frame = requestAnimationFrame(() => position(state));
        });
        state.observer.observe(anchor);
        if (popup()) state.observer.observe(popup());
    };
}

export function update(id, options) {
    const state = hints.get(id);
    if (!state) return;
    if (state.options.hover !== options.hover) state.suppressed = false;
    state.options = options;
    const target = state.anchor.querySelector('button, a[href], [tabindex]');
    target?.setAttribute('aria-haspopup', 'dialog');
    target?.setAttribute('aria-expanded', String(options.open));
    if (options.open) target?.setAttribute('aria-controls', id);
    else target?.removeAttribute('aria-controls');
    const popup = document.getElementById(id);
    if (options.open && popup) {
        if (popup.showPopover && !popup.matches(':popover-open')) popup.showPopover();
        position(state);
        state.observe();
    } else {
        clearTimeout(state.timer);
        state.observer?.disconnect();
        if (popup?.hidePopover && popup.matches(':popover-open')) popup.hidePopover();
    }
}

export function disconnect(id) {
    const state = hints.get(id);
    if (!state) return;
    state.cleanup();
    state.observer?.disconnect();
    clearTimeout(state.timer);
    cancelAnimationFrame(state.frame);
    const target = state.anchor.querySelector('button, a[href], [tabindex]');
    for (const attribute of ['aria-haspopup', 'aria-controls', 'aria-expanded']) target?.removeAttribute(attribute);
    hints.delete(id);
}
