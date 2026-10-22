import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const baseURL = process.env.RADZEN_URL || 'https://localhost:5084';
const output = process.env.AUDIT_OUTPUT || 'artifacts/browser-audit';
await mkdir(output, { recursive: true });
const results = [];
// Text may inherit color while SVG tick labels use fill. Resolve the painted
// surface through ancestors instead of comparing against the page background.
const textContrast = element => {
  const rgb = color => color.match(/[\d.]+/g)?.map(Number) || [0, 0, 0];
  const luminance = values => values.slice(0, 3).map(v => {
    v /= 255;
    return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
  }).reduce((sum, v, i) => sum + v * [0.2126, 0.7152, 0.0722][i], 0);
  let surface = element;
  while (surface && rgb(getComputedStyle(surface).backgroundColor)[3] === 0) surface = surface.parentElement;
  const background = rgb(surface ? getComputedStyle(surface).backgroundColor : 'rgb(255, 255, 255)');
  const foreground = rgb(getComputedStyle(element)[element instanceof SVGElement ? 'fill' : 'color']);
  const a = luminance(foreground), b = luminance(background);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
};
try {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  page.setDefaultTimeout(10000);
  page.setDefaultNavigationTimeout(30000); // First prerender/JIT after a clean source build can exceed 10 s.
  for (const theme of ['admiral-regression', 'admiral-regression-dark']) {
    for (const route of ['/datagrid', '/column-chart', '/line-chart', '/pie-chart', '/datepicker', '/dropdown', '/button', '/alert', '/alert-styling', '/skeleton']) {
      const result = { theme, route, errors: [], warnings: [] };
      const onError = e => result.errors.push(e.message);
      const onConsole = message => {
        if (message.type() === 'error' && /_content\/Radzen\.Admiral/.test(message.text())) result.errors.push(message.text());
      };
      page.on('pageerror', onError);
      page.on('console', onConsole);
      try {
        await page.goto(`${baseURL}${route}?theme=${theme}`, { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(800);
        // Use the browser certificate store, just like the actual stylesheet
        // load (Node's request client doesn't trust macOS ASP.NET dev certs).
        const css = await page.evaluate(async url => {
          const response = await fetch(url);
          return { status: response.status, length: (await response.arrayBuffer()).byteLength };
        }, `${baseURL}/css/${theme}-base.css`);
        assert.equal(css.status, 200);
        assert.ok(css.length > 100000, 'HTTP 200 must not mask an empty CSS response');
        if (route.endsWith('-chart')) {
          await page.locator('.rz-chart svg .rz-series-0').first().waitFor();
          await page.waitForTimeout(1200); // Capture the completed series entrance animation.
          result.series = await page.locator('.rz-chart svg [class*="rz-series-"]').evaluateAll(es => es.map(e => ({
            token: getComputedStyle(e).getPropertyValue('--rz-series-color').trim(),
            painted: [...e.querySelectorAll('path,rect,circle')].map(p => ({ fill: getComputedStyle(p).fill, stroke: getComputedStyle(p).stroke }))
          })).filter(s => s.token));
          assert.ok(result.series.length, 'Series palette must be resolved');
          assert.ok(result.series.some(s => s.painted.some(p => !['none', 'rgb(0, 0, 0)'].includes(p.fill) || !['none', 'rgb(0, 0, 0)'].includes(p.stroke))), 'Chart must not fall back to black');
          const tick = page.locator('.rz-chart .rz-tick-text').first();
          if (await tick.count()) {
            result.tickContrast = await tick.evaluate(textContrast);
            assert.ok(result.tickContrast >= 4.5, `Axis label contrast: ${result.tickContrast.toFixed(2)}`);
          }
        }
        if (route === '/datagrid') {
          result.summaryContrast = await page.locator('.rz-pager-summary').first().evaluate(textContrast);
          assert.ok(result.summaryContrast >= 4.5, `Pager summary contrast: ${result.summaryContrast.toFixed(2)}`);
          const grid = page.locator('.rz-data-grid-data[role="grid"]').first();
          await grid.focus(); await page.keyboard.press('ArrowDown');
          await page.waitForFunction(() => {
            const cell = document.querySelector('.rz-data-grid-data tr.rz-data-row.rz-state-focused > td');
            return cell && !['transparent', 'rgba(0, 0, 0, 0)'].includes(getComputedStyle(cell).backgroundColor);
          });
          await page.keyboard.press('ArrowRight');
          await page.waitForFunction(() => {
            const cell = document.querySelector('.rz-data-grid-data td.rz-state-focused');
            return cell && getComputedStyle(cell).outlineStyle !== 'none';
          });
        }
        if (route === '/datepicker') {
          const trigger = page.locator('.rz-datepicker-trigger').first();
          await trigger.click();
          const calendar = page.locator('.rz-datepicker-popup-container:visible').first();
          await calendar.waitFor();
          await page.waitForTimeout(250); // Let the popup entrance opacity settle before the screenshot.
          result.calendarTitleContrast = await calendar.locator('.rz-calendar-title').evaluate(textContrast);
          assert.ok(result.calendarTitleContrast >= 4.5, `Calendar title contrast: ${result.calendarTitleContrast.toFixed(2)}`);
          const next = calendar.locator('.rz-calendar-next').first();
          await next.hover();
          const geometry = await next.evaluate(e => {
            const box = e.getBoundingClientRect(), icon = e.querySelector('.rzi').getBoundingClientRect();
            return { dx: (box.left + box.right - icon.left - icon.right) / 2, dy: (box.top + box.bottom - icon.top - icon.bottom) / 2 };
          });
          assert.ok(Math.abs(geometry.dx) <= 1 && Math.abs(geometry.dy) <= 1, 'Calendar navigation icon stays centered on hover');
          result.tocContrast = await page.locator('.rz-toc-link').first().evaluate(textContrast);
          assert.ok(result.tocContrast >= 4.5, `Toc link contrast: ${result.tocContrast.toFixed(2)}`);
          await page.screenshot({ path: `${output}/radzen-${theme}-calendar-hover.png` });
          await next.focus(); await page.keyboard.press('Escape');
          await calendar.waitFor({ state: 'hidden' });
          const input = page.locator('.rz-datepicker input').first();
          await page.waitForFunction(() => document.activeElement?.matches('.rz-datepicker input'));
          await page.keyboard.press('Alt+ArrowDown');
          await page.locator('.rz-datepicker-popup-container:visible').first().waitFor();
          await input.focus(); await page.keyboard.press('Escape');
          await page.locator('.rz-datepicker-popup-container:visible').waitFor({ state: 'hidden' });
        }
        if (route === '/button') {
          result.buttonContrast = {};
          for (const variant of ['outlined', 'text']) {
            const button = page.locator(`.rz-button.rz-variant-${variant}.rz-dark.rz-shade-default`).first();
            const demoContrast = await button.evaluate(textContrast);
            // Upstream deliberately puts Light/Dark examples on base-500.
            // Record that contrast independently; don't retune a semantic color
            // to fit this arbitrary surface. Also check the normal card surface.
            await button.evaluate(e => {
              const probe = e.cloneNode(true);
              probe.removeAttribute('id');
              probe.setAttribute('data-admiral-contrast-probe', '');
              e.closest('.rz-card').append(probe);
            });
            const probe = page.locator('[data-admiral-contrast-probe]');
            let cardContrast;
            try { cardContrast = await probe.evaluate(textContrast); }
            finally { await probe.evaluate(e => e.remove()); }
            result.buttonContrast[variant] = { demoContrast, cardContrast };
            assert.ok(cardContrast >= 4.5, `Dark ${variant} button must remain readable on the card surface`);
            if (demoContrast < 4.5) result.warnings.push(`Dark ${variant} on upstream base-500 demo surface: contrast ${demoContrast.toFixed(2)} < 4.5`);
          }
        }
        if (route === '/alert-styling') {
          // At 1440 px upstream uses dropdowns; the sibling select bars are hidden.
          await page.locator('.rz-dropdown:visible').filter({ has: page.locator('input[name="AlertStyle"]') }).click();
          await page.getByRole('option', { name: 'Dark', exact: true }).click();
          for (const variant of ['Outlined', 'Text']) {
            await page.locator('.rz-dropdown:visible').filter({ has: page.locator('input[name="Variant"]') }).click();
            await page.getByRole('option', { name: variant, exact: true }).click();
            const alert = page.locator(`.rz-alert.rz-variant-${variant.toLowerCase()}.rz-dark`).first();
            await alert.waitFor();
            assert.ok(await alert.evaluate(textContrast) >= 4.5, `Dark ${variant} alert must remain readable`);
          }
        }
        await page.screenshot({ path: `${output}/radzen-${theme}-${route.slice(1)}.png` });
      } catch (error) { result.errors.push(error.message); }
      page.off('pageerror', onError);
      page.off('console', onConsole);
      results.push(result);
      console.log(theme, route, result.errors.length ? result.errors : 'PASS', ...result.warnings);
    }
  }
  await context.close();
} finally {
  await writeFile(`${output}/radzen-regression.json`, JSON.stringify(results, null, 2));
  await browser.close();
}
if (results.some(result => result.errors.length)) process.exitCode = 1;
