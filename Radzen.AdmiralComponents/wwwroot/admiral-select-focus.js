const panelSelector = '.rz-dropdown-panel, .rz-multiselect-panel';

function findAdmiralTrigger(panel) {
  if (!panel?.id?.startsWith('popup-')) return null;
  const trigger = document.getElementById(panel.id.slice('popup-'.length));
  return trigger?.matches('.admiral-dropdown, .admiral-select__control') ? trigger : null;
}

// Radzen can leave focus in the filter input as its portalled panel closes.
function restoreWhenClosed(panel, trigger) {
  let checks = 0;
  const restore = () => {
    if (!trigger.isConnected || ++checks > 20) return;
    if (panel.style.display !== 'none') {
      window.setTimeout(restore, 50);
      return;
    }
    const active = document.activeElement;
    if (active === document.body || active === null || panel.contains(active)) trigger.focus();
  };
  window.setTimeout(restore, 150);
}

export function installEscapeFocusRestore() {
  if (document.__admiralSelectEscapeFocusInstalled) return;
  document.__admiralSelectEscapeFocusInstalled = true;

  document.addEventListener('keydown', event => {
    if (event.key !== 'Escape') return;
    const panel = event.target?.closest?.(panelSelector);
    const trigger = findAdmiralTrigger(panel);
    if (trigger) restoreWhenClosed(panel, trigger);
  }, true);

  let viewportWidth = window.innerWidth;
  window.addEventListener('resize', () => {
    const width = window.innerWidth;
    const narrowed = width < viewportWidth;
    viewportWidth = width;
    if (!narrowed) return;

    requestAnimationFrame(() => {
      // Radzen updates the vertical position on resize but can leave the old horizontal offset.
      const radzen = window.Radzen;
      for (const popup of radzen?.popups ?? []) {
        const panel = document.getElementById(popup.id);
        if (!panel?.matches(panelSelector) || !panel.classList.contains('rz-open')) continue;
        const trigger = findAdmiralTrigger(panel);
        if (!trigger) continue;

        const { left, right } = panel.getBoundingClientRect();
        if (left >= -1 && right <= window.innerWidth + 1) continue;

        if (panel.contains(document.activeElement)) restoreWhenClosed(panel, trigger);
        radzen.closePopup(popup.id, popup.instance, popup.callback);
      }
    });
  });
}
