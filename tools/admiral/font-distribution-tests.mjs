import assert from 'node:assert/strict';
import { createReadStream, readdirSync, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { basename, extname, resolve, sep } from 'node:path';
import { createRequire } from 'node:module';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const fixtureDir = process.env.ADMIRAL_FONT_FIXTURE_DIR;
const family = 'Admiral Consumer Font Fixture';
const fixtureFamily = process.env.ADMIRAL_FONT_FIXTURE_EXPECTED_FAMILY;
const viewports = [{ width: 1280, height: 800 }, { width: 390, height: 800 }];
const themes = ['admiral', 'admiral-dark'];
const results = [];

// Private, external fixtures are served only for the optional consumer opt-in
// scenario. The test never copies licensed font bytes into this repository.
let fixtureServer;
let fixtureURL;
if (fixtureDir) {
  assert.ok(fixtureFamily,
    'Set ADMIRAL_FONT_FIXTURE_EXPECTED_FAMILY to the internal family name of the external fixture');
  const directory = resolve(fixtureDir);
  const repository = resolve(import.meta.dirname, '../..');
  assert.ok(directory !== repository && !directory.startsWith(repository + sep),
    'ADMIRAL_FONT_FIXTURE_DIR must be outside the repository');
  const files = readdirSync(directory).filter(name => /\.(otf|ttf|woff2?)$/i.test(name));
  const choose = name => files.find(file => new RegExp(`(?:^|[-_])${name}(?:\\.|[-_])`, 'i').test(file));
  const fonts = [[400, choose('Regular')], [500, choose('Medium')], [550, choose('SemiBold')]]
    .filter(([, file]) => file);
  assert.deepEqual(fonts.map(([weight]) => weight), [400, 500, 550],
    'Fixture needs Regular, Medium and SemiBold fonts');
  const css = fonts.map(([weight, file]) =>
    `@font-face{font-family:"${family}";src:url("./${encodeURIComponent(file)}");font-weight:${weight};font-style:normal;font-display:swap}`)
    .join('\n') + `\n:root{--admiral-font-family:"${family}","Admiral CJK",system-ui,sans-serif}`;
  fixtureServer = createServer((request, response) => {
    const pathname = new URL(request.url, 'http://localhost').pathname;
    response.setHeader('Access-Control-Allow-Origin', '*');
    response.setHeader('Cache-Control', 'no-store');
    if (pathname === '/font-opt-in.css') {
      response.writeHead(200, { 'Content-Type': 'text/css' }).end(css);
      return;
    }
    const filename = decodeURIComponent(pathname.slice(1));
    if (!files.includes(filename) || basename(filename) !== filename) {
      response.writeHead(404).end();
      return;
    }
    const file = resolve(directory, filename);
    response.writeHead(200, { 'Content-Type': extname(file).toLowerCase() === '.otf'
      ? 'font/otf' : extname(file).toLowerCase() === '.ttf' ? 'font/ttf' : 'font/woff2',
      'Content-Length': statSync(file).size });
    createReadStream(file).pipe(response);
  });
  await new Promise(done => fixtureServer.listen(0, '127.0.0.1', done));
  fixtureURL = `http://127.0.0.1:${fixtureServer.address().port}`;
}

const browser = await chromium.launch({ headless: true,
  ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });

// Computed font-family only reports the CSS stack. Render probes with the
// target's computed typography and ask Chromium which face painted each glyph.
async function renderedFonts(page, parentSelector) {
  const samples = { latin: 'Admiral', cyrillic: 'Адмирал', chinese: '中文测试', korean: '한국어' };
  await page.evaluate(({ selector, samples }) => {
    const parent = selector.endsWith(':visible')
      ? [...document.querySelectorAll(selector.slice(0, -8))].find(element => element.getClientRects().length)
      : document.querySelector(selector);
    if (!parent) throw new Error(`Missing font sample parent: ${selector}`);
    const style = getComputedStyle(parent);
    const container = document.createElement('span');
    container.dataset.admiralFontSamples = 'true';
    container.style.cssText = 'display:inline-flex;gap:2px;position:fixed;left:0;top:0;z-index:-1';
    container.style.fontFamily = style.fontFamily;
    container.style.fontWeight = style.fontWeight;
    container.style.fontStyle = style.fontStyle;
    container.style.fontSize = style.fontSize;
    for (const [script, value] of Object.entries(samples)) {
      const span = document.createElement('span');
      span.dataset.admiralScript = script;
      span.textContent = value;
      container.append(span);
    }
    document.body.append(container);
  }, { selector: parentSelector, samples });
  await page.evaluate(() => document.fonts.ready);
  const session = await page.context().newCDPSession(page);
  try {
    await session.send('DOM.enable');
    await session.send('CSS.enable');
    const { root } = await session.send('DOM.getDocument');
    const actual = {};
    for (const script of Object.keys(samples)) {
      const { nodeId } = await session.send('DOM.querySelector', {
        nodeId: root.nodeId,
        selector: `[data-admiral-font-samples] [data-admiral-script="${script}"]`,
      });
      assert.ok(nodeId, `CDP node for ${script}`);
      const { fonts } = await session.send('CSS.getPlatformFontsForNode', { nodeId });
      actual[script] = fonts.filter(item => item.glyphCount > 0).map(item => item.familyName);
      assert.ok(actual[script].length, `${script} glyph uses a browser-selected font`);
    }
    return actual;
  } finally {
    await session.detach();
    await page.evaluate(() => document.querySelector('[data-admiral-font-samples]')?.remove());
  }
}
try {
  for (const theme of themes) for (const viewport of viewports) {
    const context = await browser.newContext({ viewport });
    await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: baseURL }]);
    const page = await context.newPage();
    page.setDefaultTimeout(15000);
    const requests = [];
    const failed = [];
    const fixtureResponses = [];
    page.on('request', request => requests.push(request.url()));
    page.on('response', response => {
      if (fixtureURL && response.url().startsWith(fixtureURL))
        fixtureResponses.push({ url: response.url(), status: response.status() });
    });
    page.on('requestfailed', request => {
      // Blazor may abort its old circuit while a full page navigation begins.
      // The browser font gate concerns asset failures, not circuit teardown.
      if (new URL(request.url()).pathname === '/_blazor/disconnect') return;
      failed.push(`${request.url()}: ${request.failure()?.errorText}`);
    });
    const label = `${theme} ${viewport.width}px`;
    try {
      for (const route of ['/components/button', '/tables', '/components/drop-down']) {
        const response = await page.goto(baseURL + route, { waitUntil: 'domcontentloaded' });
        assert.equal(response?.status(), 200, `${route}: smoke response`);
        await page.locator('.admiral-layout').waitFor();
        await page.locator('h1').first().waitFor();
        if (fixtureURL) {
          await page.addStyleTag({ url: `${fixtureURL}/font-opt-in.css` });
          await page.evaluate(async name => {
            for (const weight of [400, 500, 550]) await document.fonts.load(`${weight} 16px "${name}"`);
          }, family);
        }
        const families = await page.evaluate(() => ({
          body: getComputedStyle(document.body).fontFamily,
          heading: getComputedStyle(document.querySelector('h1')).fontFamily,
          control: getComputedStyle(document.querySelector('.admiral-button, .rz-button, .rz-dropdown')).fontFamily,
        }));
        for (const [part, value] of Object.entries(families)) {
          assert.ok(value, `${route}: ${part} has a computed font family`);
          if (fixtureURL) assert.ok(value.includes(family), `${route}: ${part} uses consumer font: ${value}`);
        }
        if (route === '/components/button') {
          const actual = await renderedFonts(page, 'body');
          assert.ok(actual.chinese.some(name => /Noto Sans SC/i.test(name)),
            `Chinese glyph uses packaged Noto Sans SC: ${JSON.stringify(actual)}`);
          assert.ok(actual.korean.some(name => /Noto Sans KR/i.test(name)),
            `Korean glyph uses packaged Noto Sans KR: ${JSON.stringify(actual)}`);
          if (fixtureURL) {
            for (const script of ['latin', 'cyrillic']) {
              assert.ok(actual[script].some(name => name === fixtureFamily),
                `${script} glyph uses consumer font ${fixtureFamily}: ${JSON.stringify(actual)}`);
            }
          } else {
            for (const script of ['latin', 'cyrillic']) {
              assert.ok(actual[script].length > 0,
                `${script} glyph uses an available font: ${JSON.stringify(actual)}`);
            }
          }
          console.log(`${label} actual glyph fonts ${JSON.stringify(actual)}`);
        }
        if (route === '/tables') {
          const direct = page.locator('.rz-button').first();
          await direct.waitFor();
          const directFamily = await direct.evaluate(element => getComputedStyle(element).fontFamily);
          if (fixtureURL) assert.ok(directFamily.includes(family), `Direct Radzen button uses consumer font: ${directFamily}`);
          const actual = await renderedFonts(page, '.rz-button');
          assert.ok(actual.chinese.some(name => /Noto Sans SC/i.test(name)) &&
            actual.korean.some(name => /Noto Sans KR/i.test(name)),
          `Direct Radzen button CJK glyphs use packaged fonts: ${JSON.stringify(actual)}`);
          if (fixtureURL) assert.ok(actual.latin.some(name => name === fixtureFamily),
            `Direct Radzen button Latin glyphs use consumer font: ${JSON.stringify(actual)}`);
        }
        if (route === '/components/drop-down') {
          const trigger = page.locator('[data-testid="drop-down-admiral-demo"] .rz-dropdown').first();
          await trigger.waitFor();
          await trigger.click();
          const popup = page.locator('.rz-dropdown-panel:visible').last();
          await popup.waitFor();
          const popupFamily = await popup.evaluate(element => getComputedStyle(element).fontFamily);
          if (fixtureURL) assert.ok(popupFamily.includes(family), `Popup uses consumer font: ${popupFamily}`);
          const actual = await renderedFonts(page, '.rz-dropdown-panel:visible');
          assert.ok(actual.chinese.some(name => /Noto Sans SC/i.test(name)),
            `Popup Chinese glyph uses packaged Noto Sans SC: ${JSON.stringify(actual)}`);
          assert.ok(actual.korean.some(name => /Noto Sans KR/i.test(name)),
            `Popup Korean glyph uses packaged Noto Sans KR: ${JSON.stringify(actual)}`);
          if (fixtureURL) assert.ok(actual.latin.some(name => name === fixtureFamily),
            `Popup Latin glyph uses consumer font: ${JSON.stringify(actual)}`);
          await page.keyboard.press('Escape');
        }
      }
      if (fixtureURL) {
        const fontResponses = await page.evaluate(() => performance.getEntriesByType('resource')
          .filter(entry => entry.name.includes('/font-opt-in.css') || /\.(otf|ttf|woff2?)($|\?)/i.test(entry.name))
          .map(entry => entry.name));
        assert.ok(requests.some(url => url.startsWith(fixtureURL) && /\.(otf|ttf|woff2?)$/i.test(url)),
          'Browser must request consumer-hosted font bytes over HTTP');
        assert.ok(fixtureResponses.some(item => /\.(otf|ttf|woff2?)$/i.test(item.url) && item.status === 200),
          `Consumer font must load over HTTP 200: ${JSON.stringify(fixtureResponses)}`);
        assert.ok(new Set(fixtureResponses.filter(item => /\.(otf|ttf|woff2?)$/i.test(item.url) && item.status === 200)
          .map(item => item.url)).size >= 3, 'Regular, Medium and SemiBold must each load over HTTP');
        assert.ok(fixtureResponses.every(item => item.status === 200),
          `Consumer stylesheet and fonts must load: ${JSON.stringify(fixtureResponses)}`);
        assert.ok(fontResponses.some(url => url.startsWith(fixtureURL)), 'Consumer resources appear in performance entries');
        for (const weight of [400, 500, 550]) {
          assert.ok(await page.evaluate(([name, value]) => document.fonts.check(`${value} 16px "${name}"`), [family, weight]),
            `Consumer font weight ${weight} is loaded`);
        }
      }
      assert.deepEqual(failed, [], 'No failed font or application requests');
      results.push({ label, mode: fixtureURL ? 'consumer-opt-in' : 'fallback', status: 'PASS' });
    } catch (error) {
      results.push({ label, mode: fixtureURL ? 'consumer-opt-in' : 'fallback', status: 'FAIL', error: error.stack });
    } finally {
      console.log(`${label} ${fixtureURL ? 'consumer-opt-in' : 'fallback'} ${results.at(-1).status}`);
      await context.close();
    }
  }
} finally {
  await browser.close();
  if (fixtureServer) await new Promise(done => fixtureServer.close(done));
}
for (const result of results.filter(item => item.status === 'FAIL')) console.error(result.error);
assert.ok(results.every(item => item.status === 'PASS'), 'Font distribution browser regressions failed');
