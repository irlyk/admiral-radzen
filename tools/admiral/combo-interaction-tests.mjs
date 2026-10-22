import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/browser-audit';
await mkdir(output, { recursive: true });
const results = [];
try {
  for (const theme of ['admiral', 'admiral-dark']) {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    await context.addCookies([
      { name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
      { name: '.AspNetCore.Culture', value: 'c=en|uic=en', url: baseURL }
    ]);
    const page = await context.newPage();
    page.setDefaultTimeout(7000);
    const go = async route => {
      await page.goto(baseURL + route, { waitUntil: 'domcontentloaded' });
      await page.waitForTimeout(500);
    };
    const tree = page.locator('.admiral-tree-select');
    const trigger = tree.locator('.admiral-tree-select__trigger');
    const search = tree.locator('.admiral-tree-select__search');
    const node = name => tree.getByRole('treeitem', { name, exact: true });
    const active = async name => page.waitForFunction(name => {
      const control = document.querySelector('.admiral-tree-select__trigger');
      return document.getElementById(control?.getAttribute('aria-activedescendant'))?.getAttribute('aria-label') === name;
    }, name);
    const checked = async (name, value) => page.waitForFunction(({ name, value }) =>
      [...document.querySelectorAll('.admiral-tree-select [role="treeitem"]')]
        .some(e => e.getAttribute('aria-label') === name && e.getAttribute('aria-checked') === value), { name, value });
    const key = async value => { await page.keyboard.press(value); await page.waitForTimeout(70); };
    const test = async (name, run) => {
      const errors = [];
      const onError = error => errors.push(error.message);
      page.on('pageerror', onError);
      try {
        await run();
        assert.deepEqual(errors, []);
        assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
        results.push({ theme, name, passed: true });
      } catch (error) { results.push({ theme, name, passed: false, error: error.message }); }
      finally { page.off('pageerror', onError); }
      console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
    };
    await test('TreeSelect: keyboard opening, cascade normalization and mixed ARIA', async () => {
      await go('/components/tree-select');
      await trigger.focus(); await key('Space'); await active('Finance');
      await checked('Finance', 'mixed');
      await key('Enter'); await checked('Finance', 'true');
      await key('ArrowRight'); await key('ArrowDown'); await active('Payments');
      await key('ArrowRight'); await key('ArrowDown'); await active('Incoming');
      await key('Space'); await checked('Incoming', 'false');
      await checked('Payments', 'mixed'); await checked('Finance', 'mixed');
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
      await tree.locator('.admiral-tree-select__clear').click();
      await checked('Finance', 'false');
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
    });
    await test('TreeSelect: chip strategies preserve cascade selection', async () => {
      await go('/components/tree-select');
      await trigger.press('Enter');
      await trigger.press('Enter');
      await checked('Finance', 'true');
      await trigger.press('Escape');
      const values = await page.locator('.admiral-component-page__result').innerText();
      const events = await page.locator('[data-tree-events]').innerText();
      const expectChips = async expected => {
        await page.waitForFunction(expected => JSON.stringify(
          [...document.querySelectorAll('.admiral-tree-select__chip-label')].map(e => e.textContent)
        ) === JSON.stringify(expected), expected);
      };
      await expectChips(['Finance']);
      await page.getByRole('combobox', { name: 'ShowCheckedStrategy', exact: true }).selectOption('All');
      await expectChips(['Finance', 'Payments', 'Incoming', 'Outgoing', 'Reports']);
      await page.getByRole('combobox', { name: 'ShowCheckedStrategy', exact: true }).selectOption('Child');
      await expectChips(['Incoming', 'Outgoing', 'Reports']);
      assert.equal(await page.locator('.admiral-component-page__result').innerText(), values);
      await page.getByRole('combobox', { name: 'ShowCheckedStrategy', exact: true }).selectOption('Parent');
      await expectChips(['Finance']);
      assert.equal(await page.locator('[data-tree-events]').innerText(), events);
      await trigger.press('Enter'); await trigger.press('ArrowRight');
      await trigger.press('ArrowDown'); await trigger.press('ArrowRight');
      await trigger.press('ArrowDown'); await trigger.press('Space');
      await checked('Incoming', 'false'); await checked('Finance', 'mixed');
      await trigger.press('Escape');
      await expectChips(['Outgoing', 'Reports']);
      await tree.locator('.admiral-tree-select__clear').click();
      await expectChips([]);
    });
    await test('TreeSelect: removing parent chip clears its subtree and restores trigger focus', async () => {
      await go('/components/tree-select');
      await trigger.press('Enter'); await active('Finance');
      await key('Enter'); await checked('Finance', 'true'); await key('Escape');
      await tree.getByRole('button', { name: 'Удалить Finance', exact: true }).click();
      await page.waitForFunction(() => document.querySelectorAll('.admiral-tree-select__chip').length === 0);
      assert.equal((await page.locator('.admiral-component-page__result').innerText()).trim(), 'Selected:');
      assert.match(await page.locator('[data-tree-events]').innerText(), /Changes: 2/);
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
      await trigger.press('Enter'); await checked('Finance', 'false');
    });
    await test('TreeSelect: leaf chip native Space removal normalizes parent selection', async () => {
      await go('/components/tree-select');
      await trigger.press('Enter'); await active('Finance'); await key('Enter');
      await checked('Finance', 'true'); await key('Escape');
      await page.getByRole('combobox', { name: 'ShowCheckedStrategy', exact: true }).selectOption('Child');
      const remove = tree.getByRole('button', { name: 'Удалить Incoming', exact: true });
      await remove.focus(); await remove.hover();
      const style = await remove.evaluate(e => ({ outline: getComputedStyle(e).outlineStyle, background: getComputedStyle(e).backgroundColor }));
      assert.equal(style.outline, 'solid'); assert.equal(style.background, 'rgba(0, 0, 0, 0)');
      await page.screenshot({ path: `${output}/tree-select-chips-${theme}.png` });
      await key('Space');
      await page.waitForFunction(() => ![...document.querySelectorAll('.admiral-tree-select__chip-label')].some(e => e.textContent === 'Incoming'));
      assert.match(await page.locator('[data-tree-events]').innerText(), /Changes: 2/);
      await trigger.press('Enter'); await checked('Finance', 'mixed');
      await key('ArrowRight'); await key('ArrowDown'); await key('ArrowRight');
      await checked('Incoming', 'false'); await checked('Outgoing', 'true');
    });
    await test('TreeSelect: loading and skeleton block interaction without losing values', async () => {
      await go('/components/tree-select');
      await trigger.press('Enter'); await active('Finance');
      await page.getByRole('checkbox', { name: 'Loading', exact: true }).check();
      await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      assert.equal(await trigger.isDisabled(), true);
      assert.equal(await tree.getAttribute('aria-busy'), 'true');
      assert.equal(await tree.getByRole('alert', { name: 'Загрузка', exact: true }).isVisible(), true);
      assert.equal(await tree.locator('.admiral-tree-select__chip-remove').count(), 0);
      await page.getByRole('checkbox', { name: 'Loading', exact: true }).uncheck();
      await page.getByRole('checkbox', { name: 'Skeleton', exact: true }).check();
      await page.waitForFunction(() => document.querySelector('.admiral-tree-select')?.dataset.skeleton === 'true');
      assert.equal(await trigger.isDisabled(), true);
      assert.equal(await tree.locator('.admiral-tree-select__chip').count(), 0);
      const bg = await tree.locator('.admiral-tree-select__control').evaluate(e => getComputedStyle(e).backgroundColor);
      assert.notEqual(bg, 'rgba(0, 0, 0, 0)'); assert.notEqual(bg, 'rgb(255, 255, 255)');
      await page.getByRole('checkbox', { name: 'Skeleton', exact: true }).uncheck();
      await tree.getByRole('button', { name: 'Удалить Incoming', exact: true }).waitFor();
      assert.match(await page.locator('.admiral-component-page__result').innerText(), /Incoming/);
    });
    await test('TreeSelect: one chip row does not scroll; removal icons fit and are centered', async () => {
      await go('/components/tree-select');
      await page.getByRole('spinbutton', { name: 'MaxRowCount', exact: true }).fill('1');
      await page.getByRole('spinbutton', { name: 'MaxRowCount', exact: true }).press('Tab');
      await page.waitForFunction(() => getComputedStyle(document.querySelector('.admiral-tree-select__chips')).maxHeight === '24px');
      for (const size of ['Small', 'Medium', 'ExtraLarge']) {
        await page.getByRole('combobox', { name: 'Size', exact: true }).selectOption(size);
        await page.waitForFunction(size => document.querySelector('.admiral-tree-select')?.classList.contains(
          `admiral-tree-select--${({ Small: 's', Medium: 'm', ExtraLarge: 'xl' })[size]}`), size);
        const geometry = await tree.locator('.admiral-tree-select__chips').evaluate(e => {
          const button = e.querySelector('button').getBoundingClientRect();
          const icon = e.querySelector('.admiral-icon').getBoundingClientRect();
          const chip = e.querySelector('.admiral-tree-select__chip').getBoundingClientRect();
          const field = e.closest('.admiral-tree-select__control').getBoundingClientRect();
          return { scrollHeight: e.scrollHeight, clientHeight: e.clientHeight, scrollWidth: e.scrollWidth, clientWidth: e.clientWidth,
            buttonWidth: button.width, buttonHeight: button.height, iconWidth: icon.width, iconHeight: icon.height,
            dx: Math.abs(icon.x + icon.width / 2 - button.x - button.width / 2),
            dy: Math.abs(icon.y + icon.height / 2 - button.y - button.height / 2),
            fieldDy: Math.abs(chip.y + chip.height / 2 - field.y - field.height / 2) };
        });
        assert.equal(geometry.scrollHeight, geometry.clientHeight, `${size}: one row must not scroll vertically`);
        assert.equal(geometry.scrollWidth, geometry.clientWidth, `${size}: one row must not scroll horizontally`);
        assert.equal(geometry.iconWidth, 16); assert.equal(geometry.iconHeight, 16);
        assert.equal(geometry.buttonWidth, 20); assert.equal(geometry.buttonHeight, 20);
        assert.ok(geometry.dx < 0.5 && geometry.dy < 0.5, `${size}: icon must be centered`);
        assert.ok(geometry.fieldDy < 0.5, `${size}: chip row must be centered within the field`);
      }
      await tree.getByRole('button', { name: 'Удалить Incoming', exact: true }).focus();
      await tree.getByRole('button', { name: 'Удалить Incoming', exact: true }).hover();
      await tree.scrollIntoViewIfNeeded();
      await page.screenshot({ path: `${output}/tree-select-single-row-${theme}.png` });
    });
    await test('TreeSelect: row limits constrain closed chips and expand with popup', async () => {
      await go('/components/tree-select');
      await page.getByRole('button', { name: 'Select available', exact: true }).click();
      await page.getByRole('combobox', { name: 'ShowCheckedStrategy', exact: true }).selectOption('All');
      await page.getByRole('spinbutton', { name: 'MaxRowCount', exact: true }).fill('1');
      await page.getByRole('spinbutton', { name: 'MaxRowCount', exact: true }).press('Tab');
      const chips = tree.locator('.admiral-tree-select__chips');
      await page.waitForFunction(() => getComputedStyle(document.querySelector('.admiral-tree-select__chips')).maxHeight === '24px');
      assert.ok(await chips.evaluate(e => e.scrollHeight > e.clientHeight));
      await trigger.press('Enter'); await active('Finance');
      assert.equal(await chips.evaluate(e => getComputedStyle(e).maxHeight), 'none');
      await key('Escape');
      await page.getByRole('spinbutton', { name: 'MinRowCount', exact: true }).fill('2');
      await page.getByRole('spinbutton', { name: 'MinRowCount', exact: true }).press('Tab');
      await page.waitForFunction(() => getComputedStyle(document.querySelector('.admiral-tree-select__chips')).minHeight === '52px');
      assert.equal(await chips.evaluate(e => getComputedStyle(e).maxHeight), '52px');
      await page.screenshot({ path: `${output}/tree-select-rows-${theme}.png` });
    });
    await test('TreeSelect: panel button retains native keyboard behavior and Escape closes', async () => {
      await go('/components/tree-select');
      await page.getByRole('checkbox', { name: 'Panels', exact: true }).check();
      await trigger.press('Enter'); await active('Finance');
      assert.equal(await tree.getByText('Choose application sections', { exact: true }).isVisible(), true);
      await search.focus(); await key('Tab');
      const button = tree.getByRole('button', { name: 'Panel action', exact: true });
      assert.equal(await button.evaluate(e => e === document.activeElement), true);
      await key('Space');
      assert.match(await page.locator('[data-tree-events]').innerText(), /Panel action completed/);
      assert.equal(await trigger.getAttribute('aria-expanded'), 'true');
      await key('Escape'); await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
      await trigger.press('Enter'); await active('Finance');
      await tree.getByRole('button', { name: 'Удалить Incoming', exact: true }).focus();
      await key('Escape'); await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      await trigger.press('Enter'); await active('Finance');
      await tree.locator('.admiral-tree-select__clear').focus();
      await key('Escape'); await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
    });
    await test('TreeSelect: parent/child navigation, Home/End, disabled skip and read-only node', async () => {
      await go('/components/tree-select');
      await trigger.focus(); await key('ArrowDown'); await active('Finance');
      await key('End'); await active('Documents');
      await key('ArrowRight'); await key('ArrowRight'); await active('Acts');
      await key('End'); await active('Policies (read only)');
      const before = await page.locator('.admiral-component-page__result').innerText();
      await key('Enter');
      assert.equal(await page.locator('.admiral-component-page__result').innerText(), before);
      await key('ArrowUp'); await active('Statements');
      await key('ArrowLeft'); await active('Documents');
      await key('ArrowLeft');
      assert.equal(await node('Documents').getAttribute('aria-expanded'), 'false');
      await key('Home'); await active('Finance');
      await key('Escape'); await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
    });
    await test('TreeSelect: search retains text keys; Enter selects; disabled branch stays inert', async () => {
      await go('/components/tree-select');
      await trigger.click(); await search.fill('Incoming');
      await key('ArrowDown'); await active('Finance');
      await key('ArrowDown'); await active('Payments');
      await key('ArrowDown'); await active('Incoming');
      const descendant = await search.getAttribute('aria-activedescendant');
      await key('Home'); await key('ArrowRight'); await key('End');
      assert.equal(await search.inputValue(), 'Incoming');
      assert.equal(await search.getAttribute('aria-activedescendant'), descendant);
      await key('Enter'); await checked('Incoming', 'false');
      assert.equal(await search.evaluate(e => e === document.activeElement), true);
      await search.fill('Archive'); await key('ArrowDown'); await active('Documents');
      await key('ArrowDown'); await active('Documents');
      assert.equal(await node('Archive 2025').getAttribute('aria-disabled'), 'true');
      await search.fill('нет совпадений');
      await tree.getByText('Ничего не найдено', { exact: true }).waitFor();
      assert.equal(await tree.getByRole('treeitem').count(), 0);
      await key('Enter'); await key('Escape');
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
    });
    await test('TreeSelect: Tab exits, outside click does not steal focus, read-only/disabled field', async () => {
      await go('/components/tree-select');
      await trigger.click(); await search.focus(); await key('Tab');
      await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      assert.equal(await page.evaluate(() => !!document.activeElement.closest('.admiral-tree-select')), false, 'Tab must leave TreeSelect');
      await trigger.click(); await page.getByRole('checkbox', { name: 'Read-only', exact: true }).check();
      await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      await page.waitForFunction(() => document.activeElement?.closest('.admiral-component-controls'));
      await trigger.focus(); await key('Space');
      assert.equal(await trigger.getAttribute('aria-expanded'), 'false', 'Read-only trigger must not open');
      await page.getByRole('checkbox', { name: 'Read-only', exact: true }).uncheck();
      await page.getByRole('checkbox', { name: 'Disabled', exact: true }).check();
      await page.waitForFunction(() => document.querySelector('.admiral-tree-select__trigger')?.disabled);
      assert.equal(await trigger.isDisabled(), true, 'Disabled trigger must be native disabled');
    });
    await test('TreeSelect: single selection closes and restores focus', async () => {
      await go('/components/tree-select');
      await page.getByRole('checkbox', { name: 'Multiple', exact: true }).uncheck();
      await trigger.focus(); await key('ArrowUp'); await active('Documents'); await key('Enter');
      await tree.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      assert.match(await trigger.innerText(), /Documents/);
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
    });
    await test('TreeSelect: light/dark geometry, hover+focus and narrow viewport', async () => {
      await go('/components/tree-select');
      await trigger.focus(); await key('ArrowDown'); await trigger.hover();
      const control = await tree.locator('.admiral-tree-select__control').evaluate(e => {
        const s = getComputedStyle(e); return { border: s.borderTopWidth, shadow: s.boxShadow, height: e.getBoundingClientRect().height };
      });
      assert.equal(control.border, '1px'); assert.notEqual(control.shadow, 'none'); assert.equal(control.height, 40);
      const row = node('Finance').locator(':scope > .admiral-tree-select__row');
      assert.equal(await row.evaluate(e => getComputedStyle(e).outlineStyle), 'solid');
      const border = await search.evaluate(e => getComputedStyle(e).borderTopWidth);
      assert.equal(border, '1px', 'Search border must not contain a nested border shorthand');
      await page.screenshot({ path: `${output}/tree-select-${theme}.png` });
      await key('Escape');
      for (const [size, height] of [['Small', 32], ['ExtraLarge', 56]]) {
        await page.getByRole('combobox', { name: 'Size', exact: true }).selectOption(size);
        assert.equal(await tree.locator('.admiral-tree-select__control').evaluate(e => e.getBoundingClientRect().height), height);
      }
      await page.setViewportSize({ width: 600, height: 900 });
      await trigger.click();
      const box = await tree.locator('.admiral-tree-select__popup').boundingBox();
      assert.ok(box.x >= 0 && box.x + box.width <= 600);
      await page.screenshot({ path: `${output}/tree-select-${theme}-narrow.png` });
      await page.setViewportSize({ width: 1440, height: 1000 });
    });

    const global = page.locator('.admiral-global-search').first();
    const input = global.locator('input');
    const outcome = page.locator('[data-global-search-result]');
    await test('GlobalSearch: Enter submits typed query without a suggestion; no double submit', async () => {
      await go('/regression/overview'); await input.fill('нет совпадений'); await key('Enter');
      await global.locator('[role="listbox"]').waitFor({ state: 'detached' });
      assert.equal(await outcome.innerText(), 'Search: нет совпадений');
      assert.equal(await input.evaluate(e => e === document.activeElement), true);
    });
    await test('GlobalSearch: active descendant, ArrowUp starts at last result, no page scroll', async () => {
      await go('/regression/overview'); await input.scrollIntoViewIfNeeded(); await input.focus();
      const y = await page.evaluate(() => scrollY);
      await key('ArrowUp');
      let id = await input.getAttribute('aria-activedescendant');
      assert.ok(id?.endsWith('-option-4'));
      await key('ArrowDown');
      id = await input.getAttribute('aria-activedescendant');
      assert.ok(id?.endsWith('-option-0'));
      assert.equal(await page.evaluate(() => scrollY), y);
      await key('Enter');
      assert.equal(await outcome.innerText(), 'Selected: payment-1042');
      assert.equal(await input.evaluate(e => e === document.activeElement), true);
      assert.equal(await global.getByRole('listbox').count(), 0);
    });
    await test('GlobalSearch: mouse selection, clear focus, native button Space and Tab exit', async () => {
      await go('/regression/overview'); await input.fill('ADM');
      await global.getByRole('option').nth(1).click();
      await page.waitForFunction(() => document.querySelector('[data-global-search-result]')?.textContent?.includes('payment-1043'));
      assert.equal(await outcome.innerText(), 'Selected: payment-1043');
      await global.locator('.admiral-global-search__clear').click();
      await page.waitForFunction(() => document.querySelector('.admiral-global-search input')?.value === '');
      assert.equal(await input.inputValue(), '');
      await page.waitForFunction(() => document.activeElement?.classList.contains('admiral-global-search__input'));
      await input.fill('Aurora');
      await global.locator('.admiral-global-search__clear').waitFor();
      await key('Tab'); assert.equal(await global.locator('.admiral-global-search__clear').evaluate(e => e === document.activeElement), true, 'First Tab must focus clear');
      await key('Tab'); assert.equal(await global.locator('.admiral-global-search__submit').evaluate(e => e === document.activeElement), true, 'Second Tab must focus submit');
      await key('Space'); assert.equal(await outcome.innerText(), 'Search: Aurora');
      await input.focus(); await key('Tab'); await key('Tab'); await key('Tab');
      await global.locator('[role="listbox"]').waitFor({ state: 'detached' });
      assert.equal(await page.evaluate(() => !!document.activeElement.closest('.admiral-global-search')), false);
    });
    await test('GlobalSearch: Escape/outside focus; composition Enter does not submit', async () => {
      await go('/regression/overview'); await input.fill('ADM');
      await input.dispatchEvent('keydown', { key: 'Enter', code: 'Enter', isComposing: true });
      assert.equal(await outcome.innerText(), '—');
      await key('Escape'); assert.equal(await global.getByRole('listbox').count(), 0);
      await key('ArrowDown');
      await page.getByRole('button', { name: 'Open drawer', exact: true }).focus();
      await global.locator('[role="listbox"]').waitFor({ state: 'detached' });
    });
    await test('GlobalSearch: clear/submit focus visible, no hover background on clear', async () => {
      await go('/regression/overview'); await input.fill('ADM');
      const clear = global.locator('.admiral-global-search__clear'); await clear.waitFor();
      await key('Tab'); await clear.hover();
      assert.equal(await clear.evaluate(e => e === document.activeElement), true);
      const style = await clear.evaluate(e => ({ outline: getComputedStyle(e).outlineStyle, bg: getComputedStyle(e).backgroundColor }));
      assert.equal(style.outline, 'solid'); assert.equal(style.bg, 'rgba(0, 0, 0, 0)');
      await key('Tab');
      const focus = await global.locator('.admiral-global-search__submit').evaluate(e => ({ outline: getComputedStyle(e).outlineColor, bg: getComputedStyle(e).backgroundColor }));
      assert.notEqual(focus.outline, focus.bg);
      await page.screenshot({ path: `${output}/global-search-${theme}.png` });
    });
    await context.close();
  }
} finally {
  await writeFile(`${output}/combo-interactions.json`, JSON.stringify(results, null, 2));
  await browser.close();
}
if (results.some(result => !result.passed)) process.exitCode = 1;
