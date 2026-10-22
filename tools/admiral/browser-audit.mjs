// Read-only gallery state inventory. It records evidence, not a blanket parity verdict.
// PLAYWRIGHT_MODULE can point to an existing playwright / playwright-core installation.
import { createRequire } from 'node:module';
import { readFile, readdir, mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const output = path.resolve(process.env.AUDIT_OUTPUT || path.join(root, 'artifacts/browser-audit'));
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
await mkdir(output, { recursive: true });
const pages = path.join(root, 'samples/AdmiralConsumerSmoke/Pages');
const routes = [];
for (const file of await readdir(pages)) {
  if (!file.endsWith('.razor')) continue;
  const source = await readFile(path.join(pages, file), 'utf8');
  const route = source.match(/@page "([^"]+)"/)?.[1];
  if (route && !['/icons', '/coverage'].includes(route)) routes.push(route);
}
const results = [];
try {
  for (const theme of ['admiral', 'admiral-dark']) {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: baseURL }]);
    const page = await context.newPage();
    page.setDefaultTimeout(5000);
    for (const route of routes.sort()) {
      const result = { theme, route, errors: [], probes: [] };
      const onError = error => result.errors.push(error.message);
      page.on('pageerror', onError);
      try {
        await page.goto(baseURL + route, { waitUntil: 'domcontentloaded' });
        await page.waitForFunction(() => document.querySelector('.admiral-layout'));
        await page.waitForTimeout(350);
        // Representative live examples and the independent direct-Radzen playground.
        for (const tab of ['admiral', 'theme']) {
          const tabButton = page.locator(`.component-doc-tabs [data-doc-tab="${tab}"]`);
          if (await tabButton.count()) await tabButton.click();
          else if (tab !== 'admiral') continue;
          await page.waitForTimeout(150);
          const candidates = page.locator('.admiral-layout__body button, .admiral-layout__body input, .admiral-layout__body textarea, .admiral-layout__body [tabindex="0"], .admiral-layout__body a[href]');
          const seen = new Set();
          for (let i = 0; i < await candidates.count(); i++) {
            const target = candidates.nth(i);
            if (!await target.isVisible() || !await target.isEnabled()) continue;
            // Composite controls can keep an aria-hidden input under their visible
            // field. It is not an interaction target and may be pointer-covered.
            if (await target.evaluate(e => e.getAttribute('aria-hidden') === 'true')) continue;
            if (await target.evaluate(e => getComputedStyle(e).pointerEvents === 'none')) continue;
            const identity = await target.evaluate(e => `${e.tagName}.${e.className}:${e.getAttribute('role') || e.type || ''}`);
            if (seen.has(identity) || /side-menu|doc-tabs|component-controls/.test(identity)) continue;
            seen.add(identity);
            const snapshot = () => target.evaluate(e => {
              const s = getComputedStyle(e);
              return { color: s.color, background: s.backgroundColor, border: s.border, radius: s.borderRadius,
                outline: s.outline, offset: s.outlineOffset, shadow: s.boxShadow, fontWeight: s.fontWeight,
                focused: document.activeElement === e, focusVisible: e.matches(':focus-visible') };
            });
            const probe = { tab, identity, label: await target.getAttribute('aria-label'), default: await snapshot() };
            await target.hover();
            probe.hover = await snapshot();
            await page.keyboard.press('Tab'); // Keyboard modality before programmatic targeting.
            await target.focus();
            probe.hoverFocus = await snapshot();
            await page.mouse.move(0, 0);
            probe.focus = await snapshot();
            result.probes.push(probe);
          }
        }
        result.errorOverlay = await page.locator('#blazor-error-ui').isVisible();
      } catch (error) { result.errors.push(error.message); }
      page.off('pageerror', onError);
      results.push(result);
      console.log(`${theme} ${route}: ${result.probes.length} state probes, ${result.errors.length} errors`);
    }
    await context.close();
  }
} finally {
  await writeFile(path.join(output, 'gallery-states.json'), JSON.stringify(results, null, 2));
  await browser.close();
}
if (results.some(result => result.errors.length || result.errorOverlay)) process.exitCode = 1;
