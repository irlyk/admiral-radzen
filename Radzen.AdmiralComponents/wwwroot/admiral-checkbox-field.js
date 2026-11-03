export function connectLabel(label) {
    label.addEventListener('click', event => {
        const checkbox = label.querySelector(':scope > [role="checkbox"]');
        const target = event.target;
        const interactive = target.closest?.('a[href], button, input, select, textarea, [tabindex], [contenteditable]');
        if (!checkbox || checkbox.contains(target) ||
            (interactive && interactive !== label && label.contains(interactive))) {
            return;
        }

        // A native label would focus Radzen's aria-hidden input, where Space can toggle twice.
        event.preventDefault();
        if (checkbox.getAttribute('aria-disabled') === 'true') return;
        checkbox.focus();
        checkbox.click();
    });
}
