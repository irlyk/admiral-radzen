const scopes = new Map();
const focusable = root => [...root.querySelectorAll('button, a[href], input, select, textarea, [tabindex]')]
    .filter(e => !e.disabled && e.tabIndex >= 0 && e.getClientRects().length && !e.closest('[inert]'));

export function connect(id, root, restoreFocus = true) {
    disconnect(id);
    if (!root) return;
    const previous = document.activeElement;
    const keydown = event => {
        if (event.key !== 'Tab' || event.defaultPrevented) return;
        const items = focusable(root);
        const first = items[0], last = items.at(-1);
        if (!first) { event.preventDefault(); root.focus(); return; }
        if (event.shiftKey && (document.activeElement === first || document.activeElement === root)) {
            event.preventDefault(); last.focus();
        } else if (!event.shiftKey && (document.activeElement === last || document.activeElement === root)) {
            event.preventDefault(); first.focus();
        }
    };
    root.addEventListener('keydown', keydown);
    // Client-side cleanup also covers navigation/circuit loss after the DOM is removed.
    const observer = new MutationObserver(() => { if (!root.isConnected) disconnect(id); });
    observer.observe(document.body, { childList: true, subtree: true });
    scopes.set(id, { root, keydown, previous, observer, restoreFocus });
    (focusable(root)[0] || root).focus({ preventScroll: true });
}

export function setRestoreFocus(id, restoreFocus) {
    const scope = scopes.get(id);
    if (scope) scope.restoreFocus = restoreFocus;
}

export function disconnect(id) {
    const scope = scopes.get(id);
    if (!scope) return;
    scopes.delete(id);
    scope.observer.disconnect();
    scope.root.removeEventListener('keydown', scope.keydown);
    if (scope.restoreFocus && scope.previous?.isConnected &&
        (scope.root.contains(document.activeElement) || document.activeElement === document.body)) {
        scope.previous.focus({ preventScroll: true });
    }
}
