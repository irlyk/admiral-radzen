import { createRequire } from 'node:module';
import assert from 'node:assert/strict';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const base = process.env.SMOKE_URL || 'http://localhost:5072';
const routes = [
  '/components/button', '/components/icon-button', '/components/text-button', '/components/button-groups',
  '/components/icon-button-group',
  '/components/floating-button-menu', '/components/action-bar', '/components/checkbox', '/components/checkbox-group',
  '/components/text-input', '/components/text-area', '/components/number-input', '/components/select',
  '/components/input-field', '/components/drop-down', '/components/date-range-picker', '/components/tree-select',
  '/components/slider', '/components/phone-number-input', '/components/file-input', '/components/global-search',
  '/components/link', '/components/menu', '/components/drawer', '/components/stepper', '/components/tree',
  '/components/pagination', '/components/typography', '/components/divider', '/components/badge',
  '/components/chips', '/components/pills', '/components/tag', '/components/lists', '/components/scroll-container',
  '/components/carousel', '/components/image-viewer', '/tables', '/components/avatar',
  '/components/status-indicator', '/components/grid', '/components/skeleton', '/components/tooltip',
  '/components/hint', '/components/toast', '/components/progress', '/components/spinner'
];
let failures = 0;
const browser = await chromium.launch({ headless: true });
try {
  for (const theme of ['admiral', 'admiral-dark']) {
    for (const language of ['ru', 'en']) {
      const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
      await context.addCookies([
        { name: 'RadzenAdmiralTheme', value: theme, url: base },
        { name: '.AspNetCore.Culture', value: `c=${language}|uic=${language}`, url: base }
      ]);
      const page = await context.newPage();
      page.setDefaultTimeout(10000);
      const errors = [];
      page.on('pageerror', error => errors.push(error.message));
      for (const route of routes) {
        try {
          const response = await page.goto(base + route, { waitUntil: 'domcontentloaded' });
          assert.equal(response?.status(), 200, `${route}: HTTP status`);
          await page.locator('.admiral-component-page').waitFor();
          assert.equal(await page.locator('.admiral-component-page').count(), 1, `${route}: page`);
          for (const tab of ['admiral', 'api', 'theme']) {
            assert.equal(await page.locator(`.admiral-component-page .component-doc-tabs [data-doc-tab="${tab}"]`).count(), 1,
              `${route}: ${tab} tab`);
          }
          assert.equal(await page.locator('.admiral-component-page a[href*="admiralds.github.io/react-ui/"], .admiral-component-page a[href*="github.com/AdmiralDS/react-ui"]').count() > 0, true, `${route}: official link`);
          assert.deepEqual(errors.splice(0), [], `${route}: runtime errors`);
        } catch (error) {
          failures++;
          console.error(`${theme} ${language} ${route} FAIL: ${error.message}`);
        }
      }
      try {
        const response = await page.goto(base + '/icons', { waitUntil: 'domcontentloaded' });
        assert.equal(response?.status(), 200, '/icons: HTTP status');
        const section = page.locator('#icon-component-heading').locator('..');
        assert.equal(await section.getByRole('heading', { name: 'AdmiralIcon', exact: true }).count(), 1);
        assert.equal(await section.locator('a[href*="admiralds.github.io/react-ui/"]').count(), 1);
        for (const tab of ['admiral', 'api', 'theme']) {
          assert.equal(await section.locator(`[data-doc-tab="${tab}"]`).count(), 1, `/icons: ${tab} tab`);
        }
        await page.waitForTimeout(600); // Wait for the interactive Blazor circuit before the first tab click.
        await section.locator('[data-doc-tab="theme"]').click();
        await page.waitForFunction(() => document.querySelector('#icon-component-heading')?.parentElement
          ?.querySelector('[data-doc-tab="theme"]')?.getAttribute('aria-pressed') === 'true');
        await section.locator('.admiral-component-playground__preview .admiral-icon').waitFor();
        await section.locator('[data-doc-tab="admiral"]').click();
        await page.waitForFunction(() => document.querySelector('#icon-component-heading')?.parentElement
          ?.querySelector('[data-doc-tab="admiral"]')?.getAttribute('aria-pressed') === 'true');
        await section.locator('select').selectOption('ExtraLarge');
        await page.waitForFunction(() => document.querySelector('#icon-component-heading')?.parentElement
          ?.querySelector('.admiral-component-playground__preview .admiral-icon')?.getBoundingClientRect().width === 32);
        await section.locator('[data-doc-tab="api"]').click();
        await section.getByText('Razor · AdmiralIcon').waitFor();
        await page.locator('.admiral-icon-grid[role="list"]').waitFor();
        const searchName = language === 'ru' ? 'Поиск иконок' : 'Search icons';
        const categoryName = language === 'ru' ? 'Категория иконок' : 'Icon category';
        await page.getByRole('textbox', { name: searchName, exact: true }).waitFor();
        await page.getByRole('combobox', { name: categoryName, exact: true }).waitFor();
        await page.locator('.admiral-icon-grid [role="listitem"] button[aria-pressed]').first().waitFor();
        assert.deepEqual(errors.splice(0), [], '/icons: runtime errors');
      } catch (error) {
        failures++;
        console.error(`${theme} ${language} /icons FAIL: ${error.message}`);
      }
      try {
        await page.goto(base + '/components/checkbox-group', { waitUntil: 'domcontentloaded' });
        const labels = language === 'ru'
          ? ['Города', 'Москва', 'Воронеж', 'Самара', 'Омск']
          : ['Cities', 'Moscow', 'Voronezh', 'Samara', 'Omsk'];
        const preview = page.locator('.admiral-component-playground__preview');
        for (const name of labels) {
          assert.equal(await preview.getByRole('checkbox', { name, exact: true }).count(), 1,
            `/components/checkbox-group Admiral: ${name} accessible name`);
        }
        await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
        await page.waitForFunction(() => document.querySelector('.component-doc-tabs [data-doc-tab="theme"]')
          ?.getAttribute('aria-pressed') === 'true');
        for (const name of labels) {
          assert.equal(await preview.getByRole('checkbox', { name, exact: true }).count(), 1,
            `/components/checkbox-group direct Radzen: ${name} accessible name`);
        }
      } catch (error) {
        failures++;
        console.error(`${theme} ${language} /components/checkbox-group names FAIL: ${error.message}`);
      }
      for (const route of ['/components/icon-button', '/components/text-button', '/components/button-groups', '/components/icon-button-group']) {
        try {
          await page.goto(base + route, { waitUntil: 'domcontentloaded' });
          await page.waitForTimeout(600);
          const preview = page.locator('.admiral-component-playground__preview').first();
          const result = preview.locator('output');
          const before = await result.innerText();
          await preview.getByRole('button').first().focus();
          await page.keyboard.press('Enter');
          await page.waitForFunction(previous => {
            const text = document.querySelector('.admiral-component-playground__preview output')?.textContent;
            return text && text !== previous;
          }, before);
        } catch (error) {
          failures++;
          console.error(`${theme} ${language} ${route} callback FAIL: ${error.message}`);
        }
      }
      let settingsStep = 'button group secondary';
      try {
        await page.goto(base + '/components/button-groups', { waitUntil: 'networkidle' });
        await page.locator('.admiral-component-controls select').nth(1).selectOption('Secondary');
        await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview .admiral-button-group > button')
          ?.classList.contains('admiral-button--secondary'));
        settingsStep = 'button group tertiary';
        await page.locator('.admiral-component-controls select').nth(1).selectOption('Tertiary');
        await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview .admiral-button-group > button')
          ?.classList.contains('admiral-button--tertiary'));
        settingsStep = 'smoke radius';
        await page.locator('[data-testid="smoke-border-radius"]').selectOption('10');
        await page.waitForFunction(() => getComputedStyle(document.documentElement)
          .getPropertyValue('--admiral-radius-medium').trim() === '0.625rem');
        settingsStep = 'icon group dimension';
        await page.goto(base + '/components/icon-button-group', { waitUntil: 'networkidle' });
        await page.locator('.admiral-component-controls select').first().selectOption('S');
        await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview .admiral-icon-button-group > button')
          ?.classList.contains('rz-button-xs'));
      } catch (error) {
        failures++;
        console.error(`${theme} ${language} ${settingsStep} FAIL: ${error.message}`);
      }
      for (const [route, control] of [
        ['/components/chips', '.admiral-chip'],
        ['/components/action-bar', '.admiral-action-bar button']
      ]) {
        try {
          await page.goto(base + route, { waitUntil: 'domcontentloaded' });
          await page.waitForTimeout(600);
          const preview = page.locator('.admiral-component-playground__preview').first();
          const result = preview.locator('output').first();
          const before = await result.innerText();
          await preview.locator(control).first().focus();
          await page.keyboard.press('Enter');
          await page.waitForFunction(previous => {
            const text = document.querySelector('.admiral-component-playground__preview output')?.textContent;
            return text && text !== previous;
          }, before);
        } catch (error) {
          failures++;
          console.error(`${theme} ${language} ${route} callback FAIL: ${error.message}`);
        }
      }
      await page.setViewportSize({ width: 390, height: 844 });
      try {
        await page.goto(base + '/icons', { waitUntil: 'domcontentloaded' });
        await page.locator('.admiral-icon-gallery').waitFor();
        const overflow = await page.evaluate(() => document.documentElement.scrollWidth - innerWidth);
        assert.ok(overflow <= 1, `/icons: horizontal overflow ${overflow}px`);
      } catch (error) {
        failures++;
        console.error(`${theme} ${language} /icons mobile FAIL: ${error.message}`);
      }
      for (const route of routes) {
        try {
          await page.goto(base + route, { waitUntil: 'domcontentloaded' });
          await page.locator('.admiral-component-page').waitFor();
          const overflow = await page.evaluate(() => document.documentElement.scrollWidth - innerWidth);
          assert.ok(overflow <= 1, `${route}: horizontal overflow ${overflow}px`);
        } catch (error) {
          failures++;
          console.error(`${theme} ${language} ${route} mobile FAIL: ${error.message}`);
        }
      }
      console.log(`${theme} ${language}: ${routes.length} component routes + /icons, 5 keyboard callbacks, ${routes.length} component mobile pages + /icons checked`);
      await context.close();
    }
  }
} finally {
  await browser.close();
}
if (failures) process.exitCode = 1;
console.log(`Documentation completeness: ${failures ? `${failures} failures` : 'PASS'}`);
