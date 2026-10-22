// FileList synchronization deliberately does not dispatch change: Blazor's
// existing IBrowserFile stream IDs must remain valid after removal/reordering.
const selections = new WeakMap();

export function initialize(input) {
    if (selections.has(input)) return;
    selections.set(input, []);
    input.addEventListener('change', () => selections.set(input, Array.from(input.files || [])));
    const surface = input.closest('.admiral-file-input-control__dropzone');
    input.addEventListener('dragenter', () => {
        if (!input.disabled) surface?.setAttribute('data-dragover', 'true');
    });
    for (const event of ['dragleave', 'drop', 'change']) {
        input.addEventListener(event, () => surface?.removeAttribute('data-dragover'));
    }
}

export function sync(input, indices) {
    const selection = selections.get(input) || [];
    const files = indices.map(index => selection[index]).filter(Boolean);
    if (files.length === input.files.length && files.every((file, index) => file === input.files[index])) return;
    const transfer = new DataTransfer();
    for (const file of files) transfer.items.add(file);
    input.files = transfer.files;
}

export function open(input) {
    if (!input.disabled) input.click();
}
