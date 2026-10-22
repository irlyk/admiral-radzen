const registrations = new Map();
const triggers = new Map();

export function focusTrigger(anchor) {
    anchor?.querySelector('button:not(:disabled), a[href], [tabindex="0"]')?.focus({ preventScroll: true });
}

export function connectTrigger(anchor, popupId) {
    disconnectTrigger(popupId);
    const button = anchor?.querySelector('button, a[href], [tabindex="0"]');
    if (!button) return;
    button.setAttribute('aria-haspopup', 'menu');
    button.setAttribute('aria-controls', popupId);
    button.setAttribute('aria-expanded', 'false');
    const keydown = event => {
        if (button.disabled || !['ArrowDown', 'ArrowUp'].includes(event.key)) return;
        event.preventDefault();
        event.stopPropagation();
        const popup = document.getElementById(popupId);
        if (popup) return;
        anchor.dataset.menuFocusLast = String(event.key === 'ArrowUp');
        button.click();
    };
    button.addEventListener('keydown', keydown);
    triggers.set(popupId, { button, keydown });
}

export function disconnectTrigger(popupId) {
    const trigger = triggers.get(popupId);
    trigger?.button.removeEventListener('keydown', trigger.keydown);
    triggers.delete(popupId);
}

export function connectOutsideClick(trigger, popupId, dotnet) {
    disconnectOutsideClick(popupId);

    const root = trigger?.closest('.admiral-menu-root');
    const popup = document.getElementById(popupId);
    if (!popup) return;
    triggers.get(popupId)?.button.setAttribute('aria-expanded', 'true');
    const items = () => [...popup.querySelectorAll('[role="menuitem"], [role="menuitemcheckbox"]')]
        .filter(item => !item.disabled && item.getAttribute('aria-disabled') !== 'true' && item.getClientRects().length);
    const focusItem = item => {
        for (const entry of items()) entry.tabIndex = entry === item ? 0 : -1;
        item?.focus({ preventScroll: true });
        item?.scrollIntoView({ block: 'nearest' });
    };
    const keydown = event => {
        if (!popup.contains(event.target)) return;
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            dotnet.invokeMethodAsync('CloseFromDocumentAsync', true);
        } else if (event.key === 'Tab') {
            // Native Tab can leave the popup; do not restore focus over its target.
            dotnet.invokeMethodAsync('CloseFromDocumentAsync', false);
        } else if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
            event.preventDefault();
            event.stopPropagation();
            const entries = items();
            if (!entries.length) return;
            const index = entries.indexOf(document.activeElement);
            const next = event.key === 'Home' ? 0 : event.key === 'End' ? entries.length - 1
                : (index + (event.key === 'ArrowDown' ? 1 : -1) + entries.length) % entries.length;
            focusItem(entries[next]);
        }
    };
    let frame;
    const positionPopup = () => {
        const popup = document.getElementById(popupId);
        if (!popup || !root) return;

        popup.style.insetInlineStart = '0px';
        const width = popup.getBoundingClientRect().width;
        const rootLeft = root.getBoundingClientRect().left;
        const viewportLeft = window.visualViewport?.offsetLeft ?? 0;
        const viewportWidth = window.visualViewport?.width ?? window.innerWidth;
        const left = Math.max(viewportLeft + 16, Math.min(rootLeft, viewportLeft + viewportWidth - width - 16));
        popup.style.insetInlineStart = `${left - rootLeft}px`;
    };
    const schedulePosition = () => {
        cancelAnimationFrame(frame);
        frame = requestAnimationFrame(positionPopup);
    };

    const handler = event => {
        const popup = document.getElementById(popupId);
        if (!trigger?.contains(event.target) && !popup?.contains(event.target)) {
            dotnet.invokeMethodAsync('CloseFromDocumentAsync', false);
        }
    };

    positionPopup();
    const entries = items();
    focusItem(trigger.dataset.menuFocusLast === 'true' ? entries.at(-1) : entries[0]);
    delete trigger.dataset.menuFocusLast;
    popup.addEventListener('keydown', keydown);
    document.addEventListener('pointerdown', handler, true);
    document.addEventListener('scroll', schedulePosition, true);
    window.addEventListener('resize', schedulePosition);
    window.visualViewport?.addEventListener('resize', schedulePosition);
    const observer = new ResizeObserver(schedulePosition);
    if (root) observer.observe(root);
    observer.observe(document.documentElement);
    registrations.set(popupId, { popup, keydown, handler, schedulePosition, observer, cancelFrame: () => cancelAnimationFrame(frame) });
}

export function disconnectOutsideClick(popupId) {
    const registration = registrations.get(popupId);
    if (!registration) return;
    triggers.get(popupId)?.button.setAttribute('aria-expanded', 'false');
    registration.popup.removeEventListener('keydown', registration.keydown);
    document.removeEventListener('pointerdown', registration.handler, true);
    document.removeEventListener('scroll', registration.schedulePosition, true);
    window.removeEventListener('resize', registration.schedulePosition);
    window.visualViewport?.removeEventListener('resize', registration.schedulePosition);
    registration.observer.disconnect();
    registration.cancelFrame();
    registrations.delete(popupId);
}
