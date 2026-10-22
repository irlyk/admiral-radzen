export function connect(root, dotnet, mode) {
    let disposed = false;
    const isOpen = () => root.dataset.open === 'true';
    const dismiss = () => {
        if (!root.isConnected) { dispose(); return; }
        if (isOpen()) dotnet.invokeMethodAsync('DismissPopup');
    };
    const pointerdown = event => { if (!root.contains(event.target)) dismiss(); };
    const focusout = event => {
        if (!root.contains(event.relatedTarget)) dismiss();
    };
    const keydown = event => {
        if (event.isComposing || event.keyCode === 229 || event.ctrlKey || event.metaKey || event.altKey) return;
        const search = event.target.matches('input');
        if (mode === 'search' && !search) return; // Buttons retain native Enter/Space.
        if (mode === 'tree' && (root.dataset.disabled === 'true' || root.dataset.readonly === 'true')) return;
        const keys = search
            ? ['ArrowDown', 'ArrowUp', 'Enter', 'Escape']
            : ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Enter', ' ', 'Escape'];
        if (!keys.includes(event.key) || (event.key === 'Escape' && !isOpen())) return;
        if (event.key !== 'Escape' && event.target.closest('.admiral-tree-select__clear, .admiral-tree-select__chip-remove, [data-tree-panel]')) return;
        event.preventDefault();
        event.stopPropagation();
        if (event.repeat && ['Enter', ' '].includes(event.key)) return;
        dotnet.invokeMethodAsync('HandlePopupKey', event.key, search);
    };
    // Options keep DOM focus on the combobox; active descendants provide the
    // keyboard cursor. This also avoids a blur closing the popup before click.
    const optionPointerDown = event => {
        if (event.button === 0 && event.target.closest('[role="option"], .admiral-tree-select__row')) event.preventDefault();
    };
    const observer = new MutationObserver(() => {
        const id = root.querySelector('[aria-activedescendant]')?.getAttribute('aria-activedescendant');
        const active = id && document.getElementById(id);
        const viewport = root.querySelector(mode === 'tree' ? '.admiral-tree-select__tree' : '.admiral-global-search__drop');
        if (!active || !viewport) return;
        const row = active.querySelector('.admiral-tree-select__row') || active;
        const bounds = row.getBoundingClientRect(), clip = viewport.getBoundingClientRect();
        // Scroll only the popup, never the whole document.
        if (bounds.top < clip.top) viewport.scrollTop -= clip.top - bounds.top;
        else if (bounds.bottom > clip.bottom) viewport.scrollTop += bounds.bottom - clip.bottom;
    });
    observer.observe(root, { subtree: true, attributes: true, attributeFilter: ['aria-activedescendant'] });
    root.addEventListener('keydown', keydown);
    root.addEventListener('focusout', focusout);
    root.addEventListener('pointerdown', optionPointerDown);
    document.addEventListener('pointerdown', pointerdown);
    function dispose() {
        if (disposed) return;
        disposed = true;
        observer.disconnect();
        root.removeEventListener('keydown', keydown);
        root.removeEventListener('focusout', focusout);
        root.removeEventListener('pointerdown', optionPointerDown);
        document.removeEventListener('pointerdown', pointerdown);
    }
    return { dispose };
}
