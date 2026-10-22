// Radzen's Escape handler can restore focus to the filter input just as its
// portalled panel is hidden. Keep the existing combobox as the focus target.
export function installEscapeFocusRestore() {
  if (document.__admiralSelectEscapeFocusInstalled) return;
  document.__admiralSelectEscapeFocusInstalled = true;

  document.addEventListener('keydown', event => {
    if (event.key !== 'Escape') return;
    const panel = event.target?.closest?.('.rz-dropdown-panel');
    if (!panel?.id?.startsWith('popup-')) return;
    const trigger = document.getElementById(panel.id.slice('popup-'.length));
    if (!trigger?.matches?.('.admiral-dropdown, .admiral-select__control')) return;

    let checks = 0;
    const restoreWhenClosed = () => {
      if (!trigger.isConnected || ++checks > 20) return;
      if (panel.style.display !== 'none') {
        window.setTimeout(restoreWhenClosed, 50);
        return;
      }
      const active = document.activeElement;
      if (active === document.body || active === null || panel.contains(active)) trigger.focus();
    };
    window.setTimeout(restoreWhenClosed, 150);
  }, true);
}
