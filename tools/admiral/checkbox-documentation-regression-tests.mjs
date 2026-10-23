import { createRequire } from 'node:module';
import assert from 'node:assert/strict';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const base = process.env.SMOKE_URL || 'http://localhost:5072';
const browser = await chromium.launch({ headless: true });
const results = [];
try {
  for (const theme of ['admiral', 'admiral-dark']) {
    for (const language of ['ru', 'en']) {
      const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
      await context.addCookies([
        { name: 'RadzenAdmiralTheme', value: theme, url: base },
        { name: '.AspNetCore.Culture', value: `c=${language}|uic=${language}`, url: base }
      ]);
      const page = await context.newPage();
      const errors = [];
      page.on('pageerror', error => errors.push(error.message));
      const label = language === 'ru' ? 'Флажок' : 'Checkbox';
      const controls = language === 'ru'
        ? { disabled: 'Недоступно', readOnly: 'Только чтение', error: 'Ошибка', indeterminate: 'Неопределённое состояние' }
        : { disabled: 'Disabled', readOnly: 'Read-only', error: 'Error', indeterminate: 'Indeterminate' };
      try {
        await page.goto(base + '/components/checkbox', { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(600);
        const demo = page.getByTestId('checkbox-admiral-demo');
        const field = demo.locator('.admiral-checkbox-field');
        const checkbox = demo.getByRole('checkbox', { name: label, exact: true });
        assert.equal(await checkbox.count(), 1, 'Admiral checkbox has its visible label as accessible name');
        assert.equal(await checkbox.getAttribute('aria-checked'), 'false');
        assert.match(await field.getAttribute('class'), /admiral-checkbox-field--m/);
        await page.keyboard.press('Tab');
        await checkbox.focus();
        assert.equal(await checkbox.evaluate(element => element.matches(':focus-visible')), true);
        assert.notEqual(await checkbox.evaluate(element => getComputedStyle(element).outlineStyle), 'none');
        await page.keyboard.press('Space');
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-demo"] .rz-chkbox')
          ?.getAttribute('aria-checked') === 'true');
        assert.match(await demo.getByTestId('checkbox-admiral-result').innerText(), /(?:Changes|Изменений):\s*1/);

        await demo.getByRole('combobox', { name: language === 'ru' ? 'Размер' : 'Dimension' }).selectOption('S');
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
          ?.classList.contains('admiral-checkbox-field--s'));
        await demo.getByRole('combobox', { name: language === 'ru' ? 'Размер' : 'Dimension' }).selectOption('M');
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
          ?.classList.contains('admiral-checkbox-field--m'));

        await demo.getByRole('checkbox', { name: controls.disabled, exact: true }).check();
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
          ?.classList.contains('admiral-checkbox-field--disabled'));
        assert.match(await field.getAttribute('class'), /admiral-checkbox-field--disabled/);
        assert.equal(await checkbox.getAttribute('aria-disabled'), 'true');
        assert.equal(await checkbox.getAttribute('tabindex'), '-1');
        assert.match(await demo.getByTestId('checkbox-admiral-result').innerText(), /(?:Changes|Изменений):\s*1/);
        await demo.getByRole('checkbox', { name: controls.disabled, exact: true }).uncheck();

        await demo.getByRole('checkbox', { name: controls.readOnly, exact: true }).check();
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
          ?.classList.contains('admiral-checkbox-field--readonly'));
        assert.match(await field.getAttribute('class'), /admiral-checkbox-field--readonly/);
        await checkbox.focus();
        await page.keyboard.press('Space');
        assert.equal(await checkbox.getAttribute('aria-checked'), 'true');
        assert.match(await demo.getByTestId('checkbox-admiral-result').innerText(), /(?:Changes|Изменений):\s*1/);
        await demo.getByRole('checkbox', { name: controls.readOnly, exact: true }).uncheck();

        await demo.getByRole('checkbox', { name: controls.error, exact: true }).check();
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
          ?.classList.contains('admiral-checkbox-field--error'));
        assert.match(await field.getAttribute('class'), /admiral-checkbox-field--error/);
        assert.notEqual(await field.locator('.rz-chkbox-box.rz-state-active').evaluate(element =>
          getComputedStyle(element).boxShadow), 'none', 'checked + error keeps a visible error ring');
        await demo.getByRole('checkbox', { name: controls.indeterminate, exact: true }).check();
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-demo"] .rz-chkbox')
          ?.getAttribute('aria-checked') === 'mixed');

        await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
        await page.waitForFunction(() => document.querySelector('.component-doc-tabs [data-doc-tab="theme"]')
          ?.getAttribute('aria-pressed') === 'true');
        const direct = page.getByTestId('checkbox-radzen-demo');
        const directCheckbox = direct.getByRole('checkbox', { name: label, exact: true });
        assert.equal(await directCheckbox.count(), 1, 'Direct Radzen checkbox has an accessible name');
        assert.equal(await directCheckbox.getAttribute('aria-checked'), 'false');
        await page.keyboard.press('Tab');
        await directCheckbox.focus();
        assert.equal(await directCheckbox.evaluate(element => element.matches(':focus-visible')), true);
        await page.keyboard.press('Space');
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-radzen-demo"] .rz-chkbox')
          ?.getAttribute('aria-checked') === 'true');
        assert.match(await direct.getByTestId('checkbox-radzen-result').innerText(), /(?:Changes|Изменений):\s*1/);
        await direct.getByRole('checkbox', { name: controls.disabled, exact: true }).check();
        await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-radzen-demo"] .rz-chkbox')
          ?.getAttribute('aria-disabled') === 'true');
        assert.equal(await directCheckbox.getAttribute('aria-disabled'), 'true');
        assert.match(await direct.getByTestId('checkbox-radzen-result').innerText(), /(?:Changes|Изменений):\s*1/);
        assert.deepEqual(errors, []);
        results.push({ theme, language, passed: true });
      } catch (error) {
        results.push({ theme, language, passed: false, error: error.stack });
      } finally {
        await context.close();
      }
      console.log(`${theme} ${language}: ${results.at(-1).passed ? 'PASS' : results.at(-1).error}`);
    }
  }
} finally {
  await browser.close();
}
if (results.some(result => !result.passed)) process.exitCode = 1;
