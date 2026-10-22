(function () {
  const prefix = 'admiral:';
  const config = window.AdmiralRadzenIcons || {};
  const basePath = config.basePath || '_content/Radzen.Admiral.Icons/admiral-icons/';
  const iconCache = new Map();

  function normalizeIconName(value) {
    return value.substring(prefix.length).trim().replace(/\\/g, '/').replace(/^\/+/, '').replace(/\.svg$/i, '');
  }

  function normalizePreloadName(value) {
    const text = String(value || '').trim();
    return text.startsWith(prefix)
      ? normalizeIconName(text)
      : text.replace(/\\/g, '/').replace(/^\/+/, '').replace(/\.svg$/i, '');
  }

  function iconUrl(name) {
    const normalizedBase = basePath.endsWith('/') ? basePath : `${basePath}/`;
    return `${normalizedBase}${name}.svg`;
  }

  function prepareSvg(svg) {
    svg.classList.add('rzi-admiral-svg');
    svg.setAttribute('aria-hidden', 'true');
    svg.setAttribute('focusable', 'false');

    const paintedElements = svg.querySelectorAll('[fill], [stroke]');
    const paints = new Set();

    paintedElements.forEach(function (element) {
      ['fill', 'stroke'].forEach(function (attribute) {
        const paint = element.getAttribute(attribute);
        const opacity = element.getAttribute(`${attribute}-opacity`);

        if (paint && paint !== 'none' && paint !== 'transparent' && opacity !== '0') {
          paints.add(paint.trim().toLowerCase());
        }
      });
    });

    // Admiral includes both theme-aware monochrome glyphs and intrinsically
    // colored assets such as bank logos, payment systems and country flags.
    // Flattening the latter to currentColor merges their background and mark
    // into a solid rectangle, so only normalize SVGs that use one paint.
    if (paints.size > 1) {
      svg.classList.add('rzi-admiral-svg--multicolor');
      return svg;
    }

    paintedElements.forEach(function (element) {
      if (element.hasAttribute('fill') && element.getAttribute('fill') !== 'none') {
        element.setAttribute('fill', 'currentColor');
      }

      if (element.hasAttribute('stroke') && element.getAttribute('stroke') !== 'none') {
        element.setAttribute('stroke', 'currentColor');
      }
    });

    return svg;
  }

  function getSvg(name) {
    if (!iconCache.has(name)) {
      iconCache.set(name, fetch(iconUrl(name))
        .then(function (response) {
          if (!response.ok) {
            throw new Error(`Admiral icon "${name}" failed to load.`);
          }

          return response.text();
        })
        .then(function (text) {
          const template = document.createElement('template');
          template.innerHTML = text.trim();
          const svg = template.content.querySelector('svg');

          if (!svg) {
            throw new Error(`Admiral icon "${name}" is not an SVG.`);
          }

          return prepareSvg(svg);
        }));
    }

    return iconCache.get(name).then(function (svg) {
      return svg.cloneNode(true);
    });
  }

  function setInlineSvg(icon, name) {
    if (icon.dataset.admiralIconLoaded === name) {
      return;
    }

    icon.dataset.admiralIconLoading = name;

    getSvg(name)
      .then(function (svg) {
        if (icon.dataset.admiralIcon !== name) {
          return;
        }

        icon.replaceChildren(svg);
        icon.dataset.admiralIconLoaded = name;
        delete icon.dataset.admiralIconLoading;
      })
      .catch(function (error) {
        icon.dataset.admiralIconError = name;
        delete icon.dataset.admiralIconLoading;
        console.warn(error);
      });
  }

  function normalizeIcon(icon) {
    const value = (icon.textContent || '').trim();

    if (!value.startsWith(prefix)) {
      return;
    }

    const name = normalizeIconName(value);

    if (!name) {
      return;
    }

    icon.classList.add('rzi-admiral');
    icon.dataset.admiralIcon = name;
    icon.textContent = '';
    setInlineSvg(icon, name);
  }

  function normalizeIcons(root) {
    const scope = root && root.querySelectorAll ? root : document;

    if (scope.matches && scope.matches('.rzi')) {
      normalizeIcon(scope);
    }

    scope.querySelectorAll('.rzi').forEach(normalizeIcon);
  }

  function preloadIcons(values) {
    const names = Array.isArray(values) ? values : [values];
    return Promise.all(names
      .map(normalizePreloadName)
      .filter(Boolean)
      .map(getSvg));
  }

  window.AdmiralRadzenIcons = Object.assign(config, {
    normalize: normalizeIcons,
    preload: preloadIcons
  });

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () { normalizeIcons(document); });
  } else {
    normalizeIcons(document);
  }

  window.addEventListener('load', function () { normalizeIcons(document); });

  if (window.MutationObserver) {
    const observer = new MutationObserver(function (mutations) {
      mutations.forEach(function (mutation) {
        if (mutation.type === 'characterData') {
          normalizeIcons(mutation.target.parentElement);
          return;
        }

        mutation.addedNodes.forEach(function (node) {
          if (node.nodeType === 1) {
            normalizeIcons(node);
          } else if (node.nodeType === 3 && node.parentElement?.matches('.rzi')) {
            // Blazor can append an icon element and its text in separate DOM
            // mutations. Normalize the parent before the browser paints the
            // raw `admiral:...` value.
            normalizeIcon(node.parentElement);
          }
        });
      });
    });

    observer.observe(document.documentElement, { childList: true, subtree: true, characterData: true });
  }
})();
