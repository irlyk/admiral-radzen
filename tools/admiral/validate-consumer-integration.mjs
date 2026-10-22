#!/usr/bin/env node
// Run the standalone PackageReference consumer and verify real browser integration.
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { spawn, execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { createServer } from 'node:net';
import { promisify } from 'node:util';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { mkdtemp, readFile } from 'node:fs/promises';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const run = promisify(execFile);
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const projectDir = path.join(root, 'samples/AdmiralConsumerValidation');
const project = path.join(projectDir, 'AdmiralConsumerValidation.csproj');
const cache = process.env.VALIDATION_NUGET_CACHE || await mkdtemp(path.join(tmpdir(), 'admiral-validation-integration-'));
const env = { ...process.env, NUGET_PACKAGES: cache, DOTNET_CLI_TELEMETRY_OPTOUT: '1', ASPNETCORE_ENVIRONMENT: 'Development' };
if ((await readFile(project, 'utf8')).includes('ProjectReference')) throw new Error('ProjectReference is forbidden in the consumer');
for (const args of [
  ['restore', project, '--configfile', path.join(projectDir, 'NuGet.config'), '--force', '-v:q'],
  ['build', project, '--no-restore', '-nologo', '-v:q']
]) {
  const result = await run('dotnet', args, { cwd: root, env, maxBuffer: 8 * 1024 * 1024 });
  if (result.stdout.trim()) console.log(result.stdout.trim());
}
const assets = JSON.parse(await readFile(path.join(projectDir, 'obj/project.assets.json'), 'utf8'));
assert.deepEqual(Object.keys(assets.packageFolders).map(folder => path.resolve(folder)), [path.resolve(cache)]);
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
for (const [name, version] of Object.entries({
  'Radzen.Admiral.Components': '1.2.2', 'Radzen.Admiral.Icons': '1.0.3', 'Radzen.Admiral.Themes': '1.1.2'
})) {
  const local = path.join(root, 'artifacts/packages/admiral', `${name}.${version}.nupkg`);
  const restored = path.join(cache, name.toLowerCase(), version, `${name.toLowerCase()}.${version}.nupkg`);
  assert.equal(digest(await readFile(restored)), digest(await readFile(local)), `${name}: restored package differs from local artifact`);
}

const freePort = await new Promise((resolve, reject) => {
  const server = createServer();
  server.once('error', reject);
  server.listen(0, '127.0.0.1', () => {
    const port = server.address().port;
    server.close(() => resolve(port));
  });
});
const base = `http://127.0.0.1:${freePort}`;
const child = spawn('dotnet', ['run', '--project', project, '--no-build', '--no-launch-profile'], {
  cwd: root, env: { ...env, ASPNETCORE_URLS: base }, stdio: ['ignore', 'pipe', 'pipe']
});
let serverOutput = '';
child.stdout.on('data', chunk => { serverOutput += chunk.toString(); });
child.stderr.on('data', chunk => { serverOutput += chunk.toString(); });
let browser;
try {
  const deadline = Date.now() + 20000;
  while (true) {
    if (child.exitCode !== null) throw new Error(`Consumer exited early:\n${serverOutput}`);
    try {
      const response = await fetch(base);
      if (response.ok) break;
    } catch { /* server still starting */ }
    if (Date.now() > deadline) throw new Error(`Consumer did not start:\n${serverOutput}`);
    await new Promise(resolve => setTimeout(resolve, 200));
  }
  browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => {
    if (response.status() >= 400) errors.push(`HTTP ${response.status()} ${response.url()}`);
  });
  await page.goto(base);
  await page.getByRole('heading', { name: 'Package consumer validation' }).waitFor();
  await page.locator('.admiral-icon svg').waitFor();
  const lightHref = await page.locator('[data-testid="theme-link"]').getAttribute('href');
  assert.match(lightHref, /admiral-base\.css$/);
  const button = page.getByRole('button', { name: 'Count clicks' });
  const buttonColor = () => button.evaluate(element => getComputedStyle(element).backgroundColor);
  const lightButtonColor = await buttonColor();
  // The icon may load during static prerender; wait for the interactive circuit.
  await page.waitForTimeout(800);
  await page.locator('[data-testid="theme-toggle"]').click();
  await page.getByText('Dark', { exact: true }).waitFor();
  assert.match(await page.locator('[data-testid="theme-link"]').getAttribute('href'), /admiral-dark-base\.css$/);
  await page.waitForFunction(color => getComputedStyle(document.querySelector('.rz-button')).backgroundColor !== color, lightButtonColor);
  const darkButtonColor = await buttonColor();
  assert.notEqual(darkButtonColor, lightButtonColor, 'Packaged light/dark CSS should alter direct RadzenButton styling');
  await button.click();
  await page.getByText('Clicks: 1', { exact: true }).waitFor();
  await page.getByPlaceholder('Search').fill('package works');
  await page.getByPlaceholder('Search').press('Tab');
  await page.getByText('Query: package works', { exact: true }).waitFor();
  await page.locator('[data-testid="theme-toggle"]').click();
  await page.getByText('Light', { exact: true }).waitFor();
  for (const asset of [
    '/_framework/blazor.web.js',
    '/_content/Radzen.Blazor/Radzen.Blazor.js',
    '/_content/Radzen.Admiral.Icons/admiral-radzen-icons.css',
    '/_content/Radzen.Admiral.Icons/admiral-radzen-icons.js',
    '/_content/Radzen.Admiral.Icons/admiral-icons/system/search-outline.svg',
    '/_content/Radzen.Admiral.Themes/css/admiral-base.css',
    '/_content/Radzen.Admiral.Themes/css/admiral-dark-base.css'
  ]) {
    assert.equal((await page.request.get(base + asset)).status(), 200, asset);
  }
  for (const theme of ['Light', 'Dark']) {
    await page.goto(base + '/pagination-regression');
    await page.getByRole('heading', { name: 'Pagination callback regression' }).waitFor();
    await page.waitForTimeout(800);
    if (theme === 'Dark') {
      await page.locator('[data-testid="pagination-theme-toggle"]').click();
      await page.getByText('Dark', { exact: true }).waitFor();
    }
    const themeHref = await page.locator('[data-testid="pagination-theme-link"]').getAttribute('href');
    assert.match(themeHref, theme === 'Dark' ? /admiral-dark-base\.css$/ : /admiral-base\.css$/);
    const model = page.locator('[data-testid="pagination-model"]');
    assert.equal((await model.innerText()).trim(), 'Page: 2 · PageSize: 10 · PageChanged: 0 · PageSizeChanged: 0');
    const size = page.locator('.admiral-pagination-one__page-size .rz-dropdown');
    await size.focus();
    await page.keyboard.press('Enter');
    await page.locator('.rz-dropdown-panel:visible').waitFor();
    await page.keyboard.press('ArrowDown');
    await page.waitForFunction(() => [...document.querySelectorAll('.rz-dropdown-panel')]
      .filter(panel => panel.getBoundingClientRect().width > 0)
      .some(panel => panel.querySelector('.rz-state-highlight')?.textContent?.trim() === '20'));
    await page.keyboard.press('Space');
    await page.waitForFunction(() => document.querySelector('[data-testid="pagination-model"]')
      ?.textContent?.includes('PageSize: 20'));
    assert.equal((await model.innerText()).trim(), 'Page: 1 · PageSize: 20 · PageChanged: 1 · PageSizeChanged: 1',
      `${theme}: changing size from 10 to 20 resets page with one callback`);
  }
  let directLightColors;
  for (const theme of ['Light', 'Dark']) {
    await page.goto(base + '/phone-regression');
    await page.getByRole('heading', { name: 'Phone package regression' }).waitFor();
    await page.waitForTimeout(800);
    if (theme === 'Dark') {
      await page.getByTestId('phone-theme-toggle').click();
      await page.getByTestId('phone-theme').getByText('Dark').waitFor();
    }
    assert.match(await page.getByTestId('phone-theme-link').getAttribute('href'),
      theme === 'Dark' ? /admiral-dark-base\.css$/ : /admiral-base\.css$/);
    const admiral = page.getByTestId('phone-admiral');
    await admiral.locator('input[inputmode="tel"]').fill('+7 777 123-45-68');
    await admiral.locator('input[inputmode="tel"]').press('Tab');
    await page.waitForFunction(() => document.querySelector('[data-testid="phone-model"]')
      ?.textContent?.includes('Value: +7 777 123-45-68'));
    assert.match(await page.getByTestId('phone-model').innerText(),
      /Country: KAZ · ValueChanged: 1 · CountryChanged: 0/,
      `${theme}: editing a shared +7 dial code retains KAZ`);
    const digits = page.getByTestId('phone-digits');
    await digits.locator('input[inputmode="tel"]').fill('+7 777 123 45 69');
    await digits.locator('input[inputmode="tel"]').press('Tab');
    await page.waitForFunction(() => document.querySelector('[data-testid="phone-digits-model"]')
      ?.textContent?.includes('Value: 77771234569'));
    assert.match(await page.getByTestId('phone-digits-model').innerText(), /Country: KAZ · CountryChanged: 0/,
      `${theme}: digits mode retains KAZ with shared +7 code`);
    const direct = page.getByTestId('phone-direct-radzen');
    const directSelect = direct.locator('.rz-dropdown');
    const directColors = await direct.evaluate(element => {
      const dropDown = element.querySelector('.rz-dropdown');
      const textBox = element.querySelector('.rz-textbox');
      return [getComputedStyle(dropDown).backgroundColor, getComputedStyle(textBox).backgroundColor];
    });
    if (theme === 'Light') directLightColors = directColors;
    else assert.notDeepEqual(directColors, directLightColors,
      'Packaged Admiral theme changes direct Radzen phone composition colors in dark mode');
    await directSelect.click();
    const popup = page.locator('.rz-dropdown-panel:visible').last();
    await popup.waitFor();
    await popup.getByText('Belarus', { exact: true }).click();
    await direct.getByPlaceholder('Direct Radzen phone').fill('+375 29 1234567');
    await direct.getByPlaceholder('Direct Radzen phone').press('Tab');
    await page.waitForFunction(() => document.querySelector('[data-testid="phone-direct-model"]')
      ?.textContent?.includes('Country: Belarus · Value: +375 29 1234567'));
    assert.equal(await directSelect.evaluate(element => getComputedStyle(element).visibility), 'visible');
  }
  for (const theme of ['Light', 'Dark']) {
    await page.goto(base + '/select-regression');
    await page.getByRole('heading', { name: 'Select package regression' }).waitFor();
    await page.waitForTimeout(800);
    if (theme === 'Dark') {
      await page.getByTestId('select-theme-toggle').click();
      await page.getByTestId('select-theme').getByText('Dark').waitFor();
    }
    assert.match(await page.getByTestId('select-theme-link').getAttribute('href'),
      theme === 'Dark' ? /admiral-dark-base\.css$/ : /admiral-base\.css$/);
    const select = page.getByTestId('select-admiral-single').locator('.rz-dropdown');
    const selectLabel = page.getByTestId('select-admiral-single').locator('.admiral-input-field__label');
    const selectLabelId = await selectLabel.getAttribute('id');
    assert.ok(selectLabelId, `${theme}: packaged SelectField label has an id`);
    assert.equal(await select.getAttribute('aria-labelledby'), selectLabelId,
      `${theme}: packaged SelectField combobox uses the visible label as its accessible name`);
    assert.equal(await page.getByTestId('select-admiral-single').getByRole('combobox', { name: 'Package city' }).count(), 1,
      `${theme}: packaged SelectField has one accessible combobox`);
    await selectLabel.click();
    await page.waitForFunction(id => document.activeElement?.id === id, await select.getAttribute('id'));
    assert.equal(await select.evaluate(element => document.activeElement === element), true,
      `${theme}: packaged SelectField label focuses the combobox`);
    await select.focus();
    await page.keyboard.press('Enter');
    const popup = page.locator('.rz-dropdown-panel:visible').last();
    await popup.waitFor();
    await popup.getByText('Kazan', { exact: true }).click();
    await page.waitForFunction(() => document.querySelector('[data-testid="select-single-model"]')
      ?.textContent?.includes('Value: Kazan'));
    assert.match(await page.getByTestId('select-single-model').innerText(), /Changes: 1/);
    assert.equal(await page.getByTestId('select-callback-log').innerText(), 'single:Kazan',
      `${theme}: controlled single selection emits one callback after updating the value`);
    await select.click();
    await popup.waitFor();
    await popup.getByText('Kazan', { exact: true }).click();
    await popup.waitFor({ state: 'hidden' });
    assert.equal(await page.getByTestId('select-callback-log').innerText(), 'single:Kazan',
      `${theme}: reselecting the same value does not emit a callback`);
    await select.click();
    await popup.waitFor();
    await popup.locator('input.rz-dropdown-filter').fill('Sochi');
    await page.waitForTimeout(250);
    assert.equal(await page.getByTestId('select-callback-log').innerText(), 'single:Kazan',
      `${theme}: filtering alone does not emit ValueChanged`);
    await page.keyboard.press('Escape');
    await popup.waitFor({ state: 'hidden' });
    await select.locator('.rz-dropdown-clear-icon').click();
    await page.waitForFunction(() => document.querySelector('[data-testid="select-callback-log"]')
      ?.textContent?.includes('single:<null>'));
    assert.equal(await page.getByTestId('select-callback-log').innerText(), 'single:Kazan | single:<null>',
      `${theme}: clear emits one null value after selection`);
    const multiple = page.getByTestId('select-admiral-multiple').locator('.rz-dropdown');
    await multiple.focus();
    await page.keyboard.press('Enter');
    const multiplePopup = page.locator('.rz-multiselect-panel:visible').last();
    await multiplePopup.waitFor();
    await multiplePopup.getByText('Sochi', { exact: true }).click();
    await page.waitForFunction(() => document.querySelector('[data-testid="select-multiple-model"]')
      ?.textContent?.includes('Sochi'));
    assert.match(await page.getByTestId('select-multiple-model').innerText(), /Changes: 1/,
      `${theme}: multiple selection updates controlled values once`);
    assert.match(await page.getByTestId('select-callback-log').innerText(), /multiple:.*Sochi/,
      `${theme}: multiple callback follows single select and clear events`);
    await page.keyboard.press('Escape');
    await multiplePopup.waitFor({ state: 'hidden' });
    const direct = page.getByTestId('select-direct-radzen').locator('.rz-dropdown');
    await direct.click();
    await popup.waitFor();
    await popup.getByText('Sochi', { exact: true }).click();
    await page.waitForFunction(() => document.querySelector('[data-testid="select-direct-model"]')
      ?.textContent?.includes('Value: Sochi'));
    assert.ok((await direct.boundingBox())?.width > 0, `${theme}: direct RadzenDropDown remains visible`);
  }
  assert.deepEqual(errors, []);
  console.log('Package consumer integration: PASS (assets, light/dark theme, AdmiralIcon, RadzenButton/TextBox, pagination callback, PhoneNumberInput, SelectField and direct RadzenDropDown)');
} finally {
  if (browser) await browser.close();
  child.kill('SIGTERM');
}
