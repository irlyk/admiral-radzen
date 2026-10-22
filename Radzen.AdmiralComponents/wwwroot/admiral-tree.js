export function connect(root) {
    let focused;
    const keydown = event => {
        if (event.isComposing) { event.stopPropagation(); return; }
        if (!event.ctrlKey && !event.metaKey && !event.altKey && !event.isComposing
            && ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End', ' ', 'Enter'].includes(event.key)) event.preventDefault();
    };
    const focusin = event => { focused = event.target.closest('[data-tree-id]'); };
    root.addEventListener('keydown', keydown);
    root.addEventListener('focusin', focusin);
    return {
        update(id) {
            if (id == null && focused && (!focused.isConnected || focused.getAttribute('aria-disabled') === 'true')) {
                if (document.activeElement === document.body || root.contains(document.activeElement)) {
                    (root.querySelector('[role="treeitem"][tabindex="0"]') || root).focus({ preventScroll: true });
                }
            }
            if (id != null) {
                const target = [...root.querySelectorAll('[data-tree-id]')].find(node => node.dataset.treeId === id);
                target?.focus({ preventScroll: true });
                target?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            }
        },
        dispose() {
            root.removeEventListener('keydown', keydown);
            root.removeEventListener('focusin', focusin);
        }
    };
}
