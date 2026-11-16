// Radzen describes the positioning wrapper. Forward that description to its actual control.
export function connect(root) {
    let control;
    let added = [];
    const tokens = element => (element?.getAttribute('aria-describedby') ?? '').split(/\s+/).filter(Boolean);
    const clear = () => {
        if (!control) return;
        const remaining = tokens(control).filter(id => !added.includes(id));
        if (remaining.length) control.setAttribute('aria-describedby', remaining.join(' '));
        else control.removeAttribute('aria-describedby');
        control = null;
        added = [];
    };
    const sync = () => {
        observer.disconnect();
        clear();
        const active = document.activeElement;
        if (active !== root) {
            control = root.contains(active) ? active : root.querySelector('button, a[href], input, select, textarea, [tabindex]');
            if (control) {
                const existing = tokens(control);
                added = tokens(root).filter(id => !existing.includes(id));
                if (added.length) control.setAttribute('aria-describedby', [...existing, ...added].join(' '));
            }
        }
        observer.observe(root, { attributes: true, attributeFilter: ['aria-describedby'], childList: true, subtree: true });
    };
    const observer = new MutationObserver(sync);
    root.addEventListener('focusin', sync);
    sync();
    return {
        disconnect() {
            observer.disconnect();
            root.removeEventListener('focusin', sync);
            clear();
        }
    };
}
