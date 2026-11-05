// Keep Radzen’s range-validation state while Admiral supplies an explicit override.
const states = new WeakMap();
const attribute = 'aria-invalid';
const observeAttribute = { attributes: true, attributeFilter: [attribute] };

export function setInvalid(root, invalid) {
    const input = root?.querySelector('input[role="spinbutton"]');
    if (!input) return;

    let state = states.get(input);
    if (state) {
        if (state.observer.takeRecords().length) state.native = input.getAttribute(attribute);
        state.observer.disconnect();
    }

    if (invalid == null) {
        if (!state) return;
        if (state.native == null) input.removeAttribute(attribute);
        else input.setAttribute(attribute, state.native);
        states.delete(input);
        return;
    }

    if (!state) {
        state = { native: input.getAttribute(attribute) };
        state.observer = new MutationObserver(() => {
            state.native = input.getAttribute(attribute);
            apply();
        });
        states.set(input, state);
    }

    state.invalid = invalid;
    apply();

    function apply() {
        state.observer.disconnect();
        input.setAttribute(attribute, state.invalid);
        state.observer.observe(input, observeAttribute);
    }
}
