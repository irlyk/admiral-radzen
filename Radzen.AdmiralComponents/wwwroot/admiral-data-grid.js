const connections = new WeakMap();

function clearMarkers(root) {
  root.querySelectorAll('.admiral-data-grid__drop-before, .admiral-data-grid__drop-after, .admiral-data-grid__dragging')
    .forEach(element => element.classList.remove(
      'admiral-data-grid__drop-before',
      'admiral-data-grid__drop-after',
      'admiral-data-grid__dragging'));
}

export function connectRowReorder(root, dotNetReference) {
  if (!root || connections.has(root)) return;

  const controller = new AbortController();
  const options = { signal: controller.signal };
  let draggedRow = null;

  root.addEventListener('dragstart', event => {
    const handle = event.target.closest('[data-admiral-row-drag-handle]');
    if (!handle || !root.contains(handle)) return;

    draggedRow = handle.closest('tr[data-admiral-row-key]');
    if (!draggedRow) return;

    draggedRow.classList.add('admiral-data-grid__dragging');
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', draggedRow.dataset.admiralRowKey);
  }, options);

  root.addEventListener('dragover', event => {
    if (!draggedRow) return;
    const targetRow = event.target.closest('tr[data-admiral-row-key]');
    if (!targetRow || targetRow === draggedRow || !root.contains(targetRow)) return;

    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
    root.querySelectorAll('.admiral-data-grid__drop-before, .admiral-data-grid__drop-after')
      .forEach(row => row.classList.remove('admiral-data-grid__drop-before', 'admiral-data-grid__drop-after'));
    const insertAfter = event.clientY >= targetRow.getBoundingClientRect().top + targetRow.offsetHeight / 2;
    targetRow.classList.add(insertAfter ? 'admiral-data-grid__drop-after' : 'admiral-data-grid__drop-before');
  }, options);

  root.addEventListener('drop', event => {
    if (!draggedRow) return;
    const targetRow = event.target.closest('tr[data-admiral-row-key]');
    if (!targetRow || targetRow === draggedRow || !root.contains(targetRow)) {
      clearMarkers(root);
      draggedRow = null;
      return;
    }

    event.preventDefault();
    const insertAfter = event.clientY >= targetRow.getBoundingClientRect().top + targetRow.offsetHeight / 2;
    const draggedKey = draggedRow.dataset.admiralRowKey;
    const targetKey = targetRow.dataset.admiralRowKey;
    clearMarkers(root);
    draggedRow = null;
    dotNetReference.invokeMethodAsync('HandleRowReorder', draggedKey, targetKey, insertAfter);
  }, options);

  root.addEventListener('dragend', () => {
    clearMarkers(root);
    draggedRow = null;
  }, options);

  connections.set(root, controller);
}

export function disconnectRowReorder(root) {
  const controller = root && connections.get(root);
  if (!controller) return;
  controller.abort();
  connections.delete(root);
  clearMarkers(root);
}
