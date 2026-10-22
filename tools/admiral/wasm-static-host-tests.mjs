import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createServer } from 'node:http';
import { createReadStream, existsSync, readFileSync, readdirSync, rmSync, statSync } from 'node:fs';
import { resolve, sep, extname, basename } from 'node:path';
import { createRequire } from 'node:module';

// Emulate GitHub Pages' project subpath and 404.html response with an ordinary
// static server. This deliberately does not use ASP.NET Core fallback routing.
const root = resolve(import.meta.dirname, '../..');
const output = resolve(root, 'artifacts/wasm-docs-publish');
const packageCache = resolve(root, 'artifacts/wasm-docs-nuget-cache');
const web = resolve(output, 'wwwroot');
const prefix = '/admiral-radzen/';
const fontFixtureDir = process.env.ADMIRAL_FONT_FIXTURE_DIR && resolve(process.env.ADMIRAL_FONT_FIXTURE_DIR);
let fontFixtureFiles = [];
if (fontFixtureDir) {
  assert.ok(fontFixtureDir !== root && !fontFixtureDir.startsWith(root + sep),
    'Font fixture must remain outside the repository');
  fontFixtureFiles = readdirSync(fontFixtureDir);
  for (const name of ['Regular', 'Medium', 'SemiBold'])
    assert.ok(fontFixtureFiles.some(file => file.includes(name)), `Missing ${name} fixture`);
}
const routes = ['/', '/components/button', '/components/checkbox', '/components/select',
  '/components/phone-number-input', '/tables', '/components/tooltip'];
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.wasm': 'application/wasm',
  '.json': 'application/json', '.css': 'text/css', '.svg': 'image/svg+xml',
  '.woff': 'font/woff', '.woff2': 'font/woff2', '.ttf': 'font/ttf', '.otf': 'font/otf',
  '.dat': 'application/octet-stream', '.dll': 'application/octet-stream',
  '.br': 'application/octet-stream', '.gz': 'application/octet-stream' };

function contrastRatio(foreground, background) {
  const luminance = color => {
    const channels = color.match(/[\d.]+/g)?.slice(0, 3).map(Number);
    assert.equal(channels?.length, 3, `Expected RGB color, received ${color}`);
    const linear = channels.map(value => {
      const normalized = value / 255;
      return normalized <= 0.04045 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
    });
    return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
  };
  const a = luminance(foreground);
  const b = luminance(background);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}

const command = ['publish', 'samples/AdmiralDocs/AdmiralDocs.csproj', '--configuration',
  'Release', '--output', output];
// Package versions intentionally remain unchanged during the pilot. A fresh
// cache proves the published app uses the latest locally rebuilt .nupkg.
rmSync(packageCache, { recursive: true, force: true });
rmSync(output, { recursive: true, force: true });
console.log(`dotnet ${command.join(' ')}`);
const build = spawnSync('dotnet', command, { cwd: root, stdio: 'inherit',
  env: { ...process.env, NUGET_PACKAGES: packageCache } });
assert.equal(build.status, 0, 'WASM publish must succeed');
for (const asset of ['index.html', '404.html', '.nojekyll', '_framework/blazor.webassembly.js',
  'css/site.css', 'css/docs.css',
  '_content/Radzen.Admiral.Themes/css/admiral-base.css',
  '_content/Radzen.Admiral.Themes/css/admiral-dark-base.css',
  '_content/Radzen.Admiral.Themes/fonts/SourceSans3VF-Upright.ttf.woff2',
  '_content/Radzen.Admiral.Icons/admiral-radzen-icons.css',
  '_content/Radzen.Admiral.Icons/admiral-icons/system/star-solid.svg']) {
  assert.ok(existsSync(resolve(web, asset)), `Missing published asset: ${asset}`);
}
assert.deepEqual(readFileSync(resolve(web, 'css/site.css')),
  readFileSync(resolve(root, 'samples/AdmiralConsumerSmoke/wwwroot/css/site.css')),
  'Published shared documentation CSS must match the smoke source');
assert.match(readFileSync(resolve(web, 'index.html'), 'utf8'),
  /<base\s+href="\/admiral-radzen\/"/i, 'Project base href must be preserved');

const server = createServer((request, response) => {
  const pathname = new URL(request.url, 'http://localhost').pathname;
  if (!pathname.startsWith(prefix)) { response.writeHead(404).end(); return; }
  const relative = decodeURIComponent(pathname.slice(prefix.length));
  if (fontFixtureDir && relative === 'css/admiral-font-opt-in.css') {
    const faces = [['Regular', 400], ['Medium', 500], ['SemiBold', 550]].map(([name, weight]) => {
      const file = fontFixtureFiles.find(item => item.includes(name));
      return `@font-face{font-family:'Admiral WASM Fixture';src:url('./fonts/${encodeURIComponent(file)}');font-weight:${weight};font-display:swap}`;
    });
    response.writeHead(200, { 'Content-Type': 'text/css' }).end(
      faces.join('\n') + "\n:root{--admiral-font-family:'Admiral WASM Fixture',system-ui,sans-serif}");
    return;
  }
  if (fontFixtureDir && relative.startsWith('css/fonts/')) {
    const name = basename(relative);
    if (!fontFixtureFiles.includes(name)) { response.writeHead(404).end(); return; }
    const file = resolve(fontFixtureDir, name);
    response.writeHead(200, { 'Content-Type': mime[extname(file)] || 'application/octet-stream',
      'Content-Length': statSync(file).size });
    createReadStream(file).pipe(response);
    return;
  }
  const candidate = resolve(web, relative || 'index.html');
  if (candidate !== web && !candidate.startsWith(web + sep)) {
    response.writeHead(403).end(); return;
  }
  let path = candidate;
  let status = 200;
  if (!existsSync(path) || pathname.endsWith('/') && relative) {
    path = resolve(web, '404.html');
    status = 404;
  }
  response.writeHead(status, { 'Content-Type': mime[extname(path)] || 'application/octet-stream',
    'Cache-Control': 'no-store' });
  createReadStream(path).pipe(response);
});
await new Promise(done => server.listen(0, '127.0.0.1', done));
const url = `http://127.0.0.1:${server.address().port}${prefix}`;
const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser = await chromium.launch({ headless: true,
  ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });

let checks = 0;
try {
  for (const viewport of [{ width: 1280, height: 850 }, { width: 390, height: 800 }]) {
    const context = await browser.newContext({ viewport });
    const page = await context.newPage();
    page.setDefaultTimeout(20000);
    const errors = [];
    const consoleErrors = new Set();
    const missing = new Set();
    const rootAssets = new Set();
    const fixtureResponses = new Set();
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => {
      if (message.type() !== 'error') return;
      const source = message.location().url;
      const sourcePath = source ? new URL(source).pathname : '';
      // Chromium can omit source URL for the expected 404.html document. Any
      // unexpected asset 404 is asserted separately from response events.
      if (/^Failed to load resource:.*404/.test(message.text()) &&
        (!sourcePath || routes.some(route => route !== '/' &&
          sourcePath === prefix.slice(0, -1) + route))) return;
      consoleErrors.add(`${message.text()} @ ${source || '(unknown)'}`);
    });
    page.on('request', request => {
      const path = new URL(request.url()).pathname;
      if (path.startsWith('/_content/') || path.startsWith('/_framework/')) rootAssets.add(path);
    });
    page.on('response', response => {
      const path = new URL(response.url()).pathname;
      if (fontFixtureDir && path.startsWith(prefix + 'css/fonts/') && response.status() === 200)
        fixtureResponses.add(path);
      if (response.status() >= 400 && !(response.status() === 404 && routes.some(route =>
        route !== '/' && path === prefix.slice(0, -1) + route))) {
        missing.add(`${response.status()} ${path}`);
      }
    });
    try {
      let response = await page.goto(url);
      assert.equal(response.status(), 200, 'Index must return 200');
      await page.getByRole('heading', { name: 'Документация компонентов' }).waitFor();
      assert.equal(await page.locator('html').getAttribute('lang'), 'ru', 'RU is default');
      const lightBackground = await page.locator('.docs-shell').evaluate(element =>
        getComputedStyle(element).backgroundColor);
      const homeLink = page.locator('#main-content a').first();
      const lightLinkColor = await homeLink.evaluate(element => getComputedStyle(element).color);
      assert.ok(contrastRatio(lightLinkColor, lightBackground) >= 4.5,
        `Light home link contrast: ${lightLinkColor} on ${lightBackground}`);
      await page.getByRole('button', { name: 'EN', exact: true }).click();
      await page.getByRole('heading', { name: 'Component documentation' }).waitFor();
      assert.equal(await page.evaluate(() => localStorage.getItem('admiral-docs-culture')), 'en');
      await page.getByRole('button', { name: 'Switch theme' }).click();
      await page.waitForFunction(() => document.getElementById('admiral-theme')?.href
        .includes('/admiral-dark-base.css'));
      assert.equal(await page.evaluate(() => localStorage.getItem('admiral-docs-theme')), 'admiral-dark');
      await page.waitForFunction(previous => getComputedStyle(document.querySelector('.docs-shell'))
        .backgroundColor !== previous, lightBackground);
      const darkBackground = await page.locator('.docs-shell').evaluate(element =>
        getComputedStyle(element).backgroundColor);
      const darkLinkColor = await homeLink.evaluate(element => getComputedStyle(element).color);
      assert.ok(contrastRatio(darkLinkColor, darkBackground) >= 4.5,
        `Dark home link contrast: ${darkLinkColor} on ${darkBackground}`);
      for (const route of routes.slice(1)) {
        response = await page.goto(url.slice(0, -1) + route);
        assert.equal(response.status(), 404, `${route}: static fallback uses 404.html`);
        await page.locator('h1').first().waitFor();
        await page.reload();
        await page.locator('h1').first().waitFor();
        assert.ok(await page.locator('.admiral-component-playground').count() > 0,
          `${route}: real shared documentation playground renders`);
        assert.equal(await page.evaluate(() => localStorage.getItem('admiral-docs-culture')), 'en');
        assert.match(await page.locator('#admiral-theme').getAttribute('href'), /admiral-dark-base\.css/);
        checks++;
      }
      await page.goto(url + 'components/button');
      const button = page.locator('.admiral-component-playground__preview button').first();
      await button.click();
      assert.match(await page.locator('.admiral-component-playground__preview').first().innerText(), /Click count:\s*1/);
      checks++;
      await page.goto(url + 'components/checkbox');
      const relatedLink = page.getByRole('link', { name: 'Checkbox groups' });
      await relatedLink.waitFor();
      assert.equal(new URL(await relatedLink.getAttribute('href'), url).pathname,
        '/admiral-radzen/components/checkbox-group', 'Shared page link stays in project subpath');
      await relatedLink.click();
      await page.getByRole('heading', { name: 'Page not yet included in the pilot' }).waitFor();
      await page.goto(url + 'components/checkbox');
      await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
      const directCheckbox = page.locator('[data-testid="checkbox-radzen-demo"] .rz-chkbox').first();
      await directCheckbox.click();
      assert.match(await page.locator('[data-testid="checkbox-radzen-result"]').innerText(),
        /Changes:\s*1/, 'Direct Radzen checkbox emits an observable callback');
      checks++;
      await page.goto(url + 'components/select');
      const combo = page.locator('.admiral-component-playground [role="combobox"]').first();
      await combo.focus();
      await page.keyboard.press('Enter');
      const popup = page.locator('.rz-dropdown-panel:visible').last();
      await popup.waitFor();
      const box = await popup.boundingBox();
      assert.ok(box && box.width > 0 && box.height > 0 && box.x >= -1 &&
        box.x + box.width <= viewport.width + 1, 'Select popup fits horizontal viewport');
      await page.keyboard.press('Escape');
      await popup.waitFor({ state: 'hidden' });
      checks++;
      if (fontFixtureDir) {
        await page.addStyleTag({ url: url + 'css/admiral-font-opt-in.css' });
        await page.evaluate(async () => {
          for (const weight of [400, 500, 550])
            await document.fonts.load(`${weight} 16px "Admiral WASM Fixture"`);
        });
        assert.match(await page.locator('body').evaluate(element => getComputedStyle(element).fontFamily),
          /Admiral WASM Fixture/, 'WASM body uses consumer font');
        assert.match(await combo.evaluate(element => getComputedStyle(element).fontFamily),
          /Admiral WASM Fixture/, 'WASM combobox uses consumer font');
        await combo.focus();
        await page.keyboard.press('Enter');
        await popup.waitFor();
        assert.match(await popup.evaluate(element => getComputedStyle(element).fontFamily),
          /Admiral WASM Fixture/, 'WASM popup uses consumer font');
        await page.keyboard.press('Escape');
        assert.equal(fixtureResponses.size, 3, 'All three consumer font weights load over HTTP');
        checks++;
      }
      await page.getByRole('button', { name: 'Switch theme' }).click();
      await page.reload();
      assert.match(await page.locator('#admiral-theme').getAttribute('href'), /admiral-base\.css$/);
      assert.deepEqual(errors, [], `Browser page errors at ${viewport.width}`);
      assert.deepEqual([...consoleErrors], [], `Browser console errors at ${viewport.width}`);
      assert.deepEqual([...missing], [], `Unexpected asset failures at ${viewport.width}`);
      assert.deepEqual([...rootAssets], [], `Package assets escaped project subpath at ${viewport.width}`);
      console.log(`WASM static host ${viewport.width}px: PASS`);
    } finally { await context.close(); }
  }
} finally {
  await browser.close();
  await new Promise(done => server.close(done));
}
console.log(`WASM static host: ${checks} browser checks PASS`);
