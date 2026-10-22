import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT;
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const results = [];

try {
  for (const theme of ['admiral', 'admiral-dark']) {
    for (const language of ['ru', 'en']) {
      const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
      await context.addCookies([
        { name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
        { name: '.AspNetCore.Culture', value: `c=${language}|uic=${language}`, url: baseURL }
      ]);
      const page = await context.newPage();
      page.setDefaultTimeout(8000);
      const go = async route => {
        await page.goto(baseURL + route, { waitUntil: 'domcontentloaded' });
        await page.locator('.admiral-layout').waitFor();
        await page.waitForTimeout(600); // Blazor hydration follows the initial document response.
      };
      const test = async (name, run) => {
        const errors = [];
        const onError = error => errors.push(error.message);
        page.on('pageerror', onError);
        try {
          await run();
          assert.deepEqual(errors, [], 'No browser runtime errors');
          assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
          results.push({ theme, language, name, passed: true });
        } catch (error) {
          results.push({ theme, language, name, passed: false, error: error.stack });
        } finally {
          page.off('pageerror', onError);
        }
        console.log(theme, language, name, results.at(-1).passed ? 'PASS' : 'FAIL');
      };

      await test('Theme survives language navigation and reload', async () => {
        const initialTheme = theme === 'admiral' ? 'admiral-dark' : 'admiral';
        await context.addCookies([{ name: 'RadzenAdmiralTheme', value: initialTheme, url: baseURL }]);
        await go('/components/button');
        const brand = page.locator('.admiral-consumer-brand');
        await brand.getByText(new RegExp(initialTheme)).waitFor();
        await page.locator('.admiral-layout__header .rz-toggle-button').click();
        await brand.getByText(new RegExp(theme)).waitFor();
        const otherLanguage = language === 'ru' ? 'en' : 'ru';
        await Promise.all([
          page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
          page.locator('.admiral-language-switcher button').filter({ hasText: otherLanguage.toUpperCase() }).click()
        ]);
        await page.locator(`.admiral-language-switcher button:has-text("${otherLanguage.toUpperCase()}")`).waitFor();
        assert.ok(['true', ''].includes(await page.locator(`.admiral-language-switcher button:has-text("${otherLanguage.toUpperCase()}")`).getAttribute('aria-pressed')));
        await brand.getByText(new RegExp(theme)).waitFor();
        await page.reload({ waitUntil: 'domcontentloaded' });
        await brand.getByText(new RegExp(theme)).waitFor();
        const cookies = await context.cookies(baseURL);
        assert.equal(cookies.find(cookie => cookie.name === 'RadzenAdmiralTheme')?.value, theme);
        assert.ok(decodeURIComponent(cookies.find(cookie => cookie.name === '.AspNetCore.Culture')?.value ?? '').includes(`uic=${otherLanguage}`));
      });

      await test('Sidebar search filters, clears and handles empty state', async () => {
        await context.addCookies([{ name: '.AspNetCore.Culture', value: `c=${language}|uic=${language}`, url: baseURL }]);
        await go('/');
        const search = page.getByTestId('component-search');
        await search.fill('Button');
        const sidebar = page.locator('.admiral-layout__side-menu');
        await sidebar.getByRole('link', { name: 'Button', exact: true }).waitFor();
        const clear = page.getByTestId('component-search-clear');
        await clear.focus();
        await clear.press('Enter');
        await page.waitForFunction(() => document.querySelector('[data-testid="component-search"]')?.value === '');
        assert.equal(await search.evaluate(element => document.activeElement === element), true,
          'Clear returns keyboard focus to component search');
        await search.fill('NoSuchAdmiralComponent123');
        await page.getByTestId('component-search-empty').waitFor();
        await page.getByTestId('component-search-clear').click();
        await page.waitForFunction(() => document.querySelector('[data-testid="component-search"]')?.value === '');
        await search.fill(language === 'ru' ? 'флажок' : 'Checkbox');
        await sidebar.getByRole('link', { name: 'Checkbox', exact: true }).waitFor();
        await page.getByTestId('component-search-clear').click();
        await page.waitForFunction(() => document.querySelector('[data-testid="component-search"]')?.value === '');
        await search.fill('Button');
        const link = sidebar.getByRole('link', { name: 'Button', exact: true });
        await link.focus();
        await link.press('Enter');
        await page.waitForURL('**/components/button');
      });

      await test('Checkbox Admiral and direct Radzen examples respond', async () => {
        await go('/components/checkbox');
        const admiral = page.getByTestId('checkbox-admiral-demo');
        await admiral.waitFor();
        const admiralResult = page.getByTestId('checkbox-admiral-result');
        const before = await admiralResult.innerText();
        const check = admiral.getByRole('checkbox').first();
        await check.focus();
        await page.keyboard.press('Space');
        await page.waitForFunction(previous => document.querySelector('[data-testid="checkbox-admiral-result"]')?.textContent !== previous, before);
        await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
        const radzen = page.getByTestId('checkbox-radzen-demo');
        await radzen.waitFor();
        const radzenResult = page.getByTestId('checkbox-radzen-result');
        const beforeRadzen = await radzenResult.innerText();
        await radzen.getByRole('checkbox').first().click();
        await page.waitForFunction(previous => document.querySelector('[data-testid="checkbox-radzen-result"]')?.textContent !== previous, beforeRadzen);
      });

      await test('Table Review action stays accessible across widths', async () => {
        await go('/tables');
        const table = page.locator('.admiral-table-playground__table');
        const review = table.locator('.admiral-data-grid__row-actions button').first();
        await review.waitFor();
        assert.ok(await review.getAttribute('aria-label'), 'Review action has an accessible name');
        for (const width of [1440, 1280, 1024, 600]) {
          await page.setViewportSize({ width, height: 900 });
          await page.waitForTimeout(350);
          const scroll = page.getByTestId('table-playground-scroll');
          if (width === 1280) {
            await scroll.evaluate(element => { element.scrollLeft = 0; });
            const initialButton = await review.boundingBox();
            const initialViewport = await scroll.boundingBox();
            assert.ok(initialButton && initialViewport &&
              initialButton.x >= initialViewport.x - 1 &&
              initialButton.x + initialButton.width <= initialViewport.x + initialViewport.width + 1,
              'Review is fully visible at 1280px without horizontal scrolling');
          }
          await review.scrollIntoViewIfNeeded();
          await review.focus();
          assert.equal(await review.isVisible(), true, `Review visible at ${width}px`);
          const box = await review.boundingBox();
          assert.ok(box && box.width > 0 && box.height > 0, `Review has a visible box at ${width}px`);
          const scrollBox = await scroll.boundingBox();
          assert.ok(scrollBox && box.x >= scrollBox.x - 1 &&
            box.x + box.width <= scrollBox.x + scrollBox.width + 1,
            `Review is fully inside the table viewport at ${width}px`);
          await review.press('Enter');
          await page.waitForFunction(() => {
            const result = document.querySelector('.admiral-table-playground__preview .admiral-component-page__result')?.textContent ?? '';
            return /action: (Review|Проверка)/i.test(result) && /event: review/i.test(result);
          });
        }
      });

      await test('PaginationOne full and simple modes appear in playground', async () => {
        await go('/tables');
        const select = page.locator('.admiral-table-playground__controls select').filter({ has: page.locator('option[value="PaginationOne"]') });
        await select.selectOption('PaginationOne');
        await page.waitForTimeout(350);
        const preview = page.locator('.admiral-table-playground__preview');
        await preview.locator('.admiral-pagination-one:not(.admiral-pagination-one--simple)').waitFor();
        assert.equal(await preview.locator('.admiral-pagination-one__page-size').isVisible(), true);
        await select.selectOption('PaginationOneSimple');
        await preview.locator('.admiral-pagination-one--simple').waitFor();
        assert.equal(await preview.locator('.admiral-pagination-one__page-size').count(), 0);
      });

      await context.close();
    }
  }
} finally {
  if (output) {
    await mkdir(output, { recursive: true });
    await writeFile(path.join(output, 'docs-iteration-regression.json'), JSON.stringify(results, null, 2));
  }
  await browser.close();
}

if (results.some(result => !result.passed)) process.exitCode = 1;
