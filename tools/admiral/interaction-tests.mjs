import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const results = [];
const output = process.env.AUDIT_OUTPUT || 'artifacts/browser-audit';
await mkdir(output, { recursive: true });
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
      await page.locator('.admiral-consumer-shell[data-interactive="true"]').waitFor();
      await page.waitForTimeout(500);
    };
    const activeText = () => page.evaluate(() => document.activeElement?.textContent.trim());
    const test = async (name, callback) => {
      const errors = [];
      const onError = error => errors.push(error.message);
      page.on('pageerror', onError);
      try {
        await callback();
        assert.deepEqual(errors, [], 'No browser runtime errors');
        assert.equal(await page.locator('#blazor-error-ui').isVisible(), false, 'No disconnected/error circuit overlay');
        results.push({ theme, name, passed: true });
      }
      catch (error) { results.push({ theme, name, passed: false, error: error.message }); }
      finally { page.off('pageerror', onError); }
      console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
    };
    await test('Menu: arrows, Home/End, checkbox, Escape, return focus, disabled skip', async () => {
      await go('/components/menu');
      const trigger = page.getByRole('button', { name: 'Open item examples', exact: true });
      await trigger.focus(); await page.keyboard.press('ArrowDown');
      await page.waitForSelector('.admiral-menu__popup');
      await page.waitForFunction(() => document.activeElement?.classList.contains('admiral-menu-item'));
      assert.match(await activeText(), /Edit payment/);
      await page.keyboard.press('ArrowDown'); assert.match(await activeText(), /Pin in recent/);
      await page.keyboard.press('Space');
      await page.waitForFunction(() => document.activeElement?.getAttribute('aria-checked') === 'true');
      await page.keyboard.press('End'); assert.match(await activeText(), /Delete/);
      await page.keyboard.press('ArrowUp'); assert.match(await activeText(), /Current operation/);
      await page.keyboard.press('Home'); assert.match(await activeText(), /Edit payment/);
      await page.keyboard.press('Escape');
      await page.waitForSelector('.admiral-menu__popup', { state: 'detached' });
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
      assert.equal(await trigger.getAttribute('aria-expanded'), 'false');
    });
    await test('Menu: Tab exits; resize keeps popup in viewport', async () => {
      await go('/components/menu');
      const trigger = page.locator('.admiral-layout__body').getByRole('button', { name: 'Actions', exact: true }).first();
      await trigger.click(); await page.waitForSelector('.admiral-menu__popup');
      await page.setViewportSize({ width: 600, height: 900 });
      await page.waitForTimeout(250);
      const bounds = await page.locator('.admiral-menu__popup').boundingBox();
      assert.ok(bounds.x >= 0 && bounds.x + bounds.width <= 600);
      await page.keyboard.press('Tab');
      await page.waitForSelector('.admiral-menu__popup', { state: 'detached' });
      await page.setViewportSize({ width: 1440, height: 1000 });
    });
    await test('Drawer: Shift+Tab/Tab trap, Escape, return focus', async () => {
      await go('/regression/overview');
      const opener = page.getByRole('button', { name: 'Open drawer', exact: true });
      await opener.click(); await page.waitForSelector('.admiral-drawer');
      await page.waitForFunction(() => document.activeElement?.closest('.admiral-drawer'));
      const initial = await page.evaluate(() => document.activeElement.getAttribute('aria-label'));
      await page.keyboard.press('Shift+Tab');
      assert.ok(await page.evaluate(() => !!document.activeElement.closest('.admiral-drawer__actions')));
      await page.keyboard.press('Tab');
      assert.equal(await page.evaluate(() => document.activeElement.getAttribute('aria-label')), initial);
      await page.keyboard.press('Escape');
      await page.waitForSelector('.admiral-drawer', { state: 'detached' });
      assert.equal(await opener.evaluate(e => e === document.activeElement), true);
    });
    await test('ImageViewer: modal focus trap and keyboard close', async () => {
      await go('/components/image-viewer');
      const opener = page.locator('.admiral-image-viewer__miniature').first();
      await opener.click(); await page.waitForSelector('.admiral-image-viewer__overlay');
      await page.waitForFunction(() => document.activeElement?.classList.contains('admiral-image-viewer__close'));
      await page.keyboard.press('Shift+Tab');
      assert.ok(await page.evaluate(() => !!document.activeElement.closest('.admiral-image-viewer__overlay')));
      await page.keyboard.press('Tab');
      assert.ok(await page.evaluate(() => document.activeElement.classList.contains('admiral-image-viewer__close')));
      await page.keyboard.press('Escape');
      await page.waitForSelector('.admiral-image-viewer__overlay', { state: 'detached' });
      assert.equal(await opener.evaluate(e => e === document.activeElement), true);
    });
    await test('GroupActionsPane: opening search actually focuses input', async () => {
      await go('/regression/overview');
      const pane = page.locator('.admiral-group-actions-pane').first();
      await pane.locator('.admiral-group-actions-pane__tools button').first().click();
      await page.waitForFunction(() => document.activeElement?.closest('.admiral-group-actions-pane__search'));
      await page.keyboard.press('Escape');
      await pane.locator('input').waitFor({ state: 'detached' });
    });
    await test('TreeSelect: Escape from popup restores trigger', async () => {
      await go('/components/tree-select');
      const trigger = page.locator('.admiral-tree-select__trigger').first();
      await trigger.click(); await page.keyboard.press('Escape');
      await page.locator('.admiral-tree-select__popup').waitFor({ state: 'detached' });
      await page.waitForFunction(() => document.activeElement?.classList.contains('admiral-tree-select__trigger'));
    });
    await test('Hint: keyboard focus stays inside; Escape returns to trigger', async () => {
      await go('/regression/overview');
      const trigger = page.getByRole('button', { name: 'Large hint', exact: true });
      await trigger.click(); await page.locator('.admiral-hint__dialog').waitFor();
      await page.waitForFunction(() => document.activeElement?.closest('.admiral-hint__dialog'));
      for (let i = 0; i < 5; i++) {
        await page.keyboard.press('Tab');
        assert.ok(await page.evaluate(() => !!document.activeElement.closest('.admiral-hint__dialog')));
      }
      await page.keyboard.press('Escape');
      await page.locator('.admiral-hint__dialog').waitFor({ state: 'detached' });
      await page.waitForFunction(() => document.activeElement?.textContent.includes('Large hint'));
    });
    await test('Grid: keyboard row focus has color, horizontal cell focus has outline', async () => {
      await go('/tables');
      const playground = page.getByTestId('table-playground-scroll');
      const grid = playground.getByRole('grid');
      await grid.press('ArrowDown');
      const row = grid.locator('tr.rz-data-row.rz-state-focused').first();
      await row.waitFor();
      await page.waitForFunction(() => {
        const cell = document.querySelector('.rz-data-grid-data tr.rz-data-row.rz-state-focused > td');
        return cell && getComputedStyle(cell).backgroundColor !== 'rgba(0, 0, 0, 0)';
      });
      const cellColor = await row.locator('td').first().evaluate(e => getComputedStyle(e).backgroundColor);
      assert.notEqual(cellColor, 'rgba(0, 0, 0, 0)');
      await page.keyboard.press('ArrowRight');
      const cell = grid.locator('td.rz-state-focused').first();
      await cell.waitFor();
      assert.notEqual(await cell.evaluate(e => getComputedStyle(e).outlineStyle), 'none');
    });
    await test('Button: Enter and Space invoke the public callback', async () => {
      await go('/components/button');
      const preview = page.locator('.admiral-component-playground__preview').first();
      await preview.locator('button').first().focus();
      await page.keyboard.press('Enter');
      await preview.getByText('Click count: 1', { exact: true }).waitFor();
      await page.keyboard.press('Space');
      await preview.getByText('Click count: 2', { exact: true }).waitFor();
    });
    await test('Button: blocked submit/reset are inert and square button has a name', async () => {
      await go('/components/button');
      const form = page.locator('.admiral-button-form-regression');
      const value = form.getByRole('textbox', { name: 'Form value' });
      const count = form.getByText('Form submit count: 0', { exact: true });
      const blocked = ['Loading', 'Skeleton', 'Disabled'];
      for (const state of blocked) {
        for (const type of ['submit', 'reset']) {
          const button = form.locator('button').filter({ hasText: `${state} ${type}` });
          assert.equal(await button.isDisabled(), true, `${state} ${type} must use native disabled`);
          if (type === 'reset') await value.fill('Changed');
          await button.evaluate(e => e.click());
          await count.waitFor();
          if (type === 'reset') assert.equal(await value.inputValue(), 'Changed', `${state} reset must not reset the form`);
        }
      }
      await value.fill('Changed');
      await form.getByRole('button', { name: 'Active reset' }).click();
      assert.equal(await value.inputValue(), 'Original');
      await form.getByRole('button', { name: 'Active submit' }).click();
      await form.getByText('Form submit count: 1', { exact: true }).waitFor();
      const square = page.getByRole('button', { name: 'Square action', exact: true });
      await square.waitFor();
      assert.equal(await square.getAttribute('aria-label'), 'Square action');
    });
    await test('TextArea: action hover is transparent, focus is square, Enter clears', async () => {
      await go('/components/text-area');
      const field = page.locator('.admiral-component-playground .admiral-input-field');
      const label = field.locator('label');
      const textarea = field.getByRole('textbox', { name: await label.innerText() });
      await label.click();
      assert.equal(await textarea.evaluate(e => e === document.activeElement), true, 'Label focuses the textarea');
      const action = field.getByRole('button', { name: 'Clear value' });
      await action.hover();
      assert.equal(await action.evaluate(e => getComputedStyle(e).backgroundColor), 'rgba(0, 0, 0, 0)');
      await page.keyboard.press('Tab'); await action.focus();
      assert.notEqual(await action.evaluate(e => getComputedStyle(e).outlineStyle), 'none');
      assert.ok(parseFloat(await action.evaluate(e => getComputedStyle(e).borderRadius)) <= 4);
      await page.keyboard.press('Enter');
      await page.waitForFunction(() => document.querySelector('textarea')?.value === '');
    });
    await test('TextArea: auto-height grows and shrinks after manual resize', async () => {
      await go('/components/text-area');
      const demo = page.locator('.admiral-component-playground');
      const textarea = demo.locator('textarea');
      await demo.getByRole('checkbox', { name: 'Resizable', exact: true }).check();
      await demo.locator('.admiral-text-area-control--resizable').waitFor();
      const bounds = await textarea.boundingBox();
      const x = bounds.x + bounds.width - 4;
      const y = bounds.y + bounds.height - 4;
      await page.mouse.move(x, y);
      await page.mouse.down();
      await page.mouse.move(x, y + 40, { steps: 6 });
      await page.mouse.up();
      assert.ok((await textarea.boundingBox()).height > bounds.height, 'Native resize changed the height');

      await demo.getByRole('checkbox', { name: 'AutoHeight', exact: true }).check();
      await demo.locator('.admiral-text-area-control--auto').waitFor();
      await textarea.fill('Short text');
      const shortHeight = (await textarea.boundingBox()).height;
      await textarea.fill('One\nTwo\nThree\nFour');
      assert.ok((await textarea.boundingBox()).height > shortHeight, 'Auto-height grows with content');
      await textarea.fill('Short text');
      assert.equal((await textarea.boundingBox()).height, shortHeight, 'Auto-height shrinks with content');
    });
    await test('NumberInput: label, literal text, steppers, bounds and clear', async () => {
      await go('/components/number-input');
      const demo = page.locator('.admiral-component-playground').first();
      const field = demo.getByRole('spinbutton', { name: 'Amount' });
      await demo.locator('.admiral-input-field__label').click();
      assert.ok(await field.evaluate(e => e === document.activeElement), 'Label focuses the spinbutton');

      const status = demo.getByRole('combobox', { name: 'Status', exact: true });
      await status.selectOption('Error');
      await page.waitForFunction(() => document.querySelector('.admiral-component-playground [role="spinbutton"]')
        ?.getAttribute('aria-invalid') === 'true');
      await status.selectOption('Success');
      await page.waitForFunction(() => !document.querySelector('.admiral-component-playground [role="spinbutton"]')
        ?.hasAttribute('aria-invalid'));

      const prefix = demo.locator('.admiral-component-controls input[type="text"], .admiral-component-controls input:not([type])').first();
      await prefix.fill("Owner's ");
      await prefix.press('Tab');
      await page.waitForFunction(() => document.querySelector('.admiral-component-playground [role="spinbutton"]')
        ?.value.startsWith("Owner's "));
      await field.fill("Owner's 5,050.50 ₽");
      await field.press('Tab');
      await demo.getByRole('status').getByText(/5050.50/).waitFor();
      await demo.getByRole('button', { name: 'Increase value' }).click();
      await demo.getByRole('status').getByText(/5051.50/).waitFor();
      await demo.getByRole('button', { name: 'Clear value' }).click();
      await page.waitForFunction(() => document.querySelector('.admiral-component-playground [role="spinbutton"]')?.value === '');

      const rate = page.getByRole('spinbutton', { name: 'Rate', exact: true });
      await rate.focus();
      await rate.press('ArrowUp');
      await page.waitForFunction(() => document.querySelector('input[aria-valuemax="100"]')?.value === '12.75 %');
      await rate.fill('150 %');
      await rate.press('Tab');
      await page.waitForFunction(() => document.querySelector('input[aria-valuemax="100"]')?.value === '100.00 %');
      await rate.fill('-10 %');
      await rate.press('Tab');
      await page.waitForFunction(() => document.querySelector('input[aria-valuemax="100"]')?.value === '0.00 %');
    });
    await test('Direct RadzenNumeric: binding, step and upper bound', async () => {
      await go('/components/number-input');
      await page.locator('[data-doc-tab="theme"]').click();
      const demo = page.locator('.admiral-component-playground').first();
      const field = demo.locator('.admiral-component-playground__preview').getByRole('spinbutton');
      const bound = demo.locator('input[type="number"]');
      await bound.waitFor();
      await field.fill('999');
      await field.press('Tab');
      await page.waitForFunction(() => document.querySelector('.admiral-component-playground input[type="number"]')?.value === '999');
      await demo.locator('.rz-numeric-up').click();
      await page.waitForFunction(() => document.querySelector('.admiral-component-playground input[type="number"]')?.value === '1000');
      await demo.locator('.rz-numeric-up').click();
      assert.equal(await bound.inputValue(), '1000');
      await demo.locator('.rz-numeric-down').click();
      await page.waitForFunction(() => document.querySelector('.admiral-component-playground input[type="number"]')?.value === '999');
    });
    await test('Slider: ArrowRight adjusts value and exposes live slider semantics', async () => {
      await go('/components/slider');
      const handle = page.locator('.admiral-slider [role="slider"]').first();
      const before = Number(await handle.getAttribute('aria-valuenow'));
      await handle.focus(); await page.keyboard.press('ArrowRight');
      await page.waitForFunction(value => Number(document.querySelector('.admiral-slider [role="slider"]').getAttribute('aria-valuenow')) > value, before);
      assert.ok(await handle.getAttribute('aria-label'));
    });
    await test('Carousel: keyboard activates next and pager controls', async () => {
      await go('/components/carousel');
      const carousel = page.locator('.admiral-carousel').first();
      const before = await carousel.locator('.rz-carousel-pager-button').evaluateAll(es => es.findIndex(e => e.classList.contains('rz-state-active')));
      await carousel.locator('.rz-carousel-next').focus(); await page.keyboard.press('Enter');
      await page.waitForFunction(index => [...document.querySelector('.admiral-carousel').querySelectorAll('.rz-carousel-pager-button')].findIndex(e => e.classList.contains('rz-state-active')) !== index, before);
      const pager = carousel.locator('.rz-carousel-pager-button').first();
      await pager.focus(); await page.keyboard.press('Space');
      await page.waitForFunction(() => document.querySelector('.admiral-carousel .rz-carousel-pager-button').classList.contains('rz-state-active'));
    });
    await test('Select: keyboard opens and Escape closes without losing trigger', async () => {
      await go('/components/select');
      const trigger = page.locator('.admiral-select [role="combobox"]').first();
      await trigger.focus(); await page.keyboard.press('Space');
      await page.locator('.rz-dropdown-panel:visible').first().waitFor();
      await page.keyboard.press('ArrowDown'); await page.keyboard.press('Escape');
      await page.locator('.rz-dropdown-panel:visible').waitFor({ state: 'hidden' });
    });
    await test('Checkbox and Switch: Space toggles; disabled controls leave tab order', async () => {
      await go('/regression/overview');
      const section = page.locator('section[aria-labelledby="binary-heading"]');
      for (const role of ['checkbox', 'switch']) {
        const target = section.getByRole(role).first();
        await target.focus(); await page.keyboard.press('Space');
        await page.waitForFunction(role => document.querySelector(`section[aria-labelledby="binary-heading"] [role="${role}"]`).getAttribute('aria-checked') === 'true', role);
        const disabled = section.locator(`[role="${role}"][tabindex="-1"]`).first();
        assert.ok(await disabled.count());
      }
    });
    await test('Radio group: arrows and Space select an enabled option', async () => {
      await go('/regression/overview');
      const group = page.locator('.admiral-radio-list').first();
      const options = group.getByRole('radio');
      await group.focus();
      await page.waitForFunction(id => document.activeElement?.getAttribute('aria-activedescendant') === id,
        await options.nth(0).getAttribute('id'));
      await group.press('ArrowDown');
      await page.waitForFunction(id => document.activeElement?.getAttribute('aria-activedescendant') === id,
        await options.nth(1).getAttribute('id'));
      await group.press('Space');
      await options.nth(1).and(group.getByRole('radio', { checked: true })).waitFor();
    });
    await test('Tabs: ArrowRight and Enter change active panel', async () => {
      await go('/regression/overview');
      const tabs = page.locator('.admiral-tabs').first();
      await tabs.getByRole('tablist').focus();
      await page.keyboard.press('ArrowRight'); await page.keyboard.press('Enter');
      await page.waitForFunction(() => document.querySelectorAll('.admiral-tabs [role="tab"]')[1].getAttribute('aria-selected') === 'true');
    });
    await test('Accordion: Enter/Space toggle; disabled header is inert', async () => {
      await go('/regression/overview');
      const header = page.locator('.rz-accordion-header button').first();
      await header.focus(); await page.keyboard.press('Enter');
      await page.waitForFunction(() => document.querySelector('.rz-accordion-header button').getAttribute('aria-expanded') === 'false');
      await page.keyboard.press('Space');
      await page.waitForFunction(() => document.querySelector('.rz-accordion-header button').getAttribute('aria-expanded') === 'true');
      assert.equal(await page.locator('.admiral-accordion-item-disabled button').isDisabled(), true);
    });
    await test('FloatingButtonMenu: hidden actions are inert; Escape returns to trigger', async () => {
      await go('/components/floating-button-menu');
      const menu = page.locator('.admiral-floating-button-menu').first();
      const trigger = menu.locator(':scope > .admiral-floating-button');
      assert.equal(await menu.locator('.admiral-floating-button-menu__items').evaluate(e => e.inert), true);
      await trigger.focus(); await page.keyboard.press('Enter');
      await page.waitForFunction(() => document.querySelector('.admiral-floating-button-menu').classList.contains('admiral-floating-button-menu--open'));
      await page.waitForTimeout(250); // Secondary buttons finish the 200 ms entrance transition.
      await page.keyboard.press('Shift+Tab');
      assert.ok(await page.evaluate(() => !!document.activeElement.closest('.admiral-floating-button-menu__items')));
      await page.keyboard.press('Escape');
      await page.waitForFunction(() => document.querySelector('.admiral-floating-button-menu__items').inert);
      assert.equal(await trigger.evaluate(e => e === document.activeElement), true);
    });
    await context.close();
  }
} finally {
  await writeFile(`${output}/interactions.json`, JSON.stringify(results, null, 2));
  await browser.close();
}
if (results.some(test => !test.passed)) process.exitCode = 1;
