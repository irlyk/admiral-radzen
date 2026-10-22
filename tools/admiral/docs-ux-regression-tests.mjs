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
      const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, permissions: ['clipboard-read', 'clipboard-write'] });
      await context.addCookies([
        { name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
        { name: '.AspNetCore.Culture', value: `c=${language}|uic=${language}`, url: baseURL }
      ]);
      const page = await context.newPage();
      page.setDefaultTimeout(8000);
      const go = async route => {
        await page.goto(baseURL + route, { waitUntil: 'domcontentloaded' });
        await page.locator('.admiral-layout').waitFor();
        await page.waitForTimeout(600);
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

      await test('Home entry and five category navigation', async () => {
        await go('/');
        assert.equal(await page.locator('.docs-home').count(), 1);
        assert.equal(await page.locator('.docs-home__category').count(), 5);
        const headings = language === 'ru'
          ? ['Действия', 'Поля ввода', 'Навигация', 'Отображение данных', 'Обратная связь']
          : ['Actions', 'Inputs', 'Navigation', 'Data Display', 'Feedback'];
        assert.deepEqual(await page.locator('.docs-home__category h2').allInnerTexts(), headings);
        const sidebar = page.locator('.admiral-side-menu');
        for (const name of headings) await sidebar.getByRole('button', { name, exact: true }).waitFor();
        const start = page.locator('.docs-home__hero-links a[href="/components/button"]');
        await start.focus();
        await start.press('Enter');
        await page.waitForURL('**/components/button');
        await go('/');
        await page.locator('.docs-home a[href="/regression/overview"]').click();
        await page.waitForURL('**/regression/overview');
        await page.locator('#binary-heading').waitFor();
        await go('/');
        await page.locator('.docs-home a[href="/guide/integration"]').click();
        await page.waitForURL('**/guide/integration');
        await page.locator('.docs-code-example').first().waitFor();
        await page.locator('.component-doc-api').first().waitFor();
      });

      await test('Theme switch has an accessible name and toggles both ways', async () => {
        await go('/');
        const toggle = page.locator('.admiral-layout__header .rz-toggle-button');
        const name = language === 'ru' ? 'Переключить цветовую тему Admiral' : 'Switch Admiral color theme';
        assert.equal(await toggle.getAttribute('aria-label'), name);
        const originalState = await toggle.getAttribute('aria-pressed');
        await toggle.focus();
        await page.keyboard.press('Space');
        await page.waitForFunction(previous => document.querySelector('.admiral-layout__header .rz-toggle-button')?.getAttribute('aria-pressed') !== previous, originalState);
        await page.keyboard.press('Space');
        await page.waitForFunction(previous => document.querySelector('.admiral-layout__header .rz-toggle-button')?.getAttribute('aria-pressed') === previous, originalState);
      });

      await test('Sidebar search handles local names and keyboard clear', async () => {
        await go('/');
        const search = page.getByTestId('component-search');
        await search.fill(language === 'ru' ? 'флажок' : 'Checkbox');
        const sidebar = page.locator('.admiral-side-menu');
        const checkbox = sidebar.getByRole('link', { name: 'Checkbox', exact: true });
        await checkbox.waitFor();
        await page.getByTestId('component-search-clear').focus();
        await page.keyboard.press('Enter');
        await page.waitForFunction(() => document.querySelector('[data-testid="component-search"]')?.value === '');
        assert.equal(await search.evaluate(element => document.activeElement === element), true);
        await search.fill('NoSuchComponent987');
        await page.getByTestId('component-search-empty').waitFor();
        await page.getByTestId('component-search-clear').click();
        await search.fill('Button');
        const button = sidebar.getByRole('link', { name: 'Button', exact: true });
        await button.focus();
        await button.press('Enter');
        await page.waitForURL('**/components/button');
      });

      await test('Pilot tabs, live callbacks and copyable Razor', async () => {
        for (const [route, kind] of [['/components/button', 'button'], ['/components/checkbox', 'checkbox'], ['/tables', 'table']]) {
          await go(route);
          await page.locator('.component-doc-tabs [data-doc-tab="admiral"]').click();
          if (kind === 'button') {
            const preview = page.locator('.admiral-component-playground__preview').first();
            await preview.getByRole('button').first().click();
            await page.waitForFunction(() => /:\s*1\b/.test(document.querySelector('.admiral-component-playground__preview .admiral-component-page__result')?.textContent ?? ''));
          } else if (kind === 'checkbox') {
            const result = page.getByTestId('checkbox-admiral-result');
            await page.getByTestId('checkbox-admiral-demo').getByRole('checkbox').first().click();
            await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-admiral-result"]')?.textContent?.includes('1'));
            assert.ok(await result.isVisible());
          } else {
            const review = page.locator('.admiral-table-playground__table .admiral-data-grid__row-actions button').first();
            await review.click();
            await page.waitForFunction(() => /Review|Проверка/.test(document.querySelector('.admiral-table-playground__preview .admiral-component-page__result')?.textContent ?? ''));
          }
          await page.locator('.docs-code-example').first().getByRole('button').click();
          await page.waitForFunction(() => /copied|скопирован/i.test(document.querySelector('.docs-code-example [role="status"]')?.textContent ?? ''));
          const copied = await page.evaluate(() => navigator.clipboard.readText());
          assert.match(copied, /<(Admiral|Radzen)/);
          await page.locator('.component-doc-tabs [data-doc-tab="api"]').click();
          await page.locator('.docs-code-example').first().waitFor();
          await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
          const direct = page.locator('.admiral-component-playground__preview').first();
          if (kind === 'button') {
            await direct.getByRole('button').first().click();
            await page.waitForFunction(() => /\b1\b/.test(document.querySelector('.admiral-component-playground__preview [role="status"]')?.textContent ?? ''));
          } else if (kind === 'checkbox') {
            await page.getByTestId('checkbox-radzen-demo').getByRole('checkbox').first().click();
            await page.waitForFunction(() => document.querySelector('[data-testid="checkbox-radzen-result"]')?.textContent?.includes('1'));
          } else {
            const status = direct.getByRole('status');
            const before = await status.innerText();
            await direct.locator('.rz-data-grid-data tbody tr').first().click();
            await page.waitForFunction(previous => {
              const current = document.querySelector('.admiral-component-playground__preview [role="status"]')?.textContent?.trim();
              return current && current !== previous.trim();
            }, before);
          }
          await page.locator('.docs-code-example').first().getByRole('button').click();
          await page.waitForFunction(() => /copied|скопирован/i.test(document.querySelector('.docs-code-example [role="status"]')?.textContent ?? ''));
        }
      });

      await test('Mobile docs navigation and page overflow', async () => {
        await page.setViewportSize({ width: 390, height: 844 });
        for (const route of ['/', '/components/button', '/components/checkbox', '/tables']) {
          await go(route);
          const overflow = await page.evaluate(() => document.documentElement.scrollWidth - innerWidth);
          assert.ok(overflow <= 1, `${route} does not overflow the 390px viewport (overflow ${overflow}px)`);
        }
        await go('/');
        await page.getByRole('button', { name: /Open navigation|Открыть навигацию/ }).click();
        const search = page.getByTestId('component-search');
        await search.waitFor();
        await search.fill('Button');
        await page.locator('.admiral-side-menu').getByRole('link', { name: 'Button', exact: true }).waitFor();
        await page.setViewportSize({ width: 1440, height: 900 });
      });

      await test('Dedicated family pages expose Admiral, API and direct alternatives', async () => {
        const routes = [
          ['/components/input-field', 'input-field-admiral-demo', 'input-field-radzen-demo'],
          ['/components/drop-down', 'drop-down-admiral-demo', 'drop-down-radzen-demo'],
          ['/components/date-range-picker', 'date-range-admiral-demo', 'date-range-radzen-demo'],
          ['/components/avatar', 'avatar-admiral-demo', null],
          ['/components/global-search', 'global-search-admiral-demo', null],
          ['/components/grid', 'grid-admiral-demo', 'grid-radzen-demo'],
          ['/components/pagination', 'pagination-admiral-demo', 'pagination-radzen-demo'],
          ['/components/progress', 'progress-admiral-demo', null],
          ['/components/spinner', 'spinner-admiral-demo', null],
          ['/components/status-indicator', 'status-indicator-admiral-demo', null]
        ];
        for (const [route, admiralDemo, radzenDemo] of routes) {
          await go(route);
          await page.getByTestId(admiralDemo).waitFor();
          await page.locator('.component-doc-tabs [data-doc-tab="api"]').click();
          await page.locator('.docs-code-example').first().waitFor();
          await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
          if (radzenDemo) {
            await page.getByTestId(radzenDemo).waitFor();
            assert.equal(await page.getByTestId(radzenDemo).locator('[class^="rz-"], [class*=" rz-"]').count() > 0, true,
              `${route} renders a direct Radzen control`);
          } else {
            await page.locator('.admiral-component-playground__preview').first().waitFor();
          }
        }
      });

      await test('PaginationOne full and simple modes keep page callbacks', async () => {
        await go('/components/pagination');
        const demo = page.getByTestId('pagination-admiral-demo');
        const pager = demo.locator('.admiral-pagination-one');
        const nextPageLabel = language === 'ru' ? 'Следующая страница' : 'Next page';
        assert.equal(await pager.locator('.admiral-pagination-one__page-size').count(), 1);
        await pager.getByRole('button', { name: nextPageLabel }).click();
        await page.waitForFunction(() => document.querySelector('[data-testid="pagination-admiral-demo"] [role="status"]')?.textContent?.includes('2'));
        await demo.getByRole('checkbox', { name: 'Simple' }).check();
        await pager.locator('.admiral-pagination-one__page-size').waitFor({ state: 'detached' });
        assert.equal(await pager.getByRole('button', { name: nextPageLabel }).count(), 1);
        await page.setViewportSize({ width: 390, height: 844 });
        await go('/components/pagination');
        const preview = page.getByTestId('pagination-admiral-demo').locator('.pagination-doc__preview');
        const geometry = await preview.evaluate(element => ({ viewport: element.clientWidth, content: element.scrollWidth, left: element.getBoundingClientRect().left }));
        assert.ok(geometry.content > geometry.viewport, 'full mode has an explicit internal horizontal scroll area on mobile');
        await preview.focus();
        await page.keyboard.press('ArrowRight');
        await page.waitForFunction(() => document.querySelector('[data-testid="pagination-admiral-demo"] .pagination-doc__preview')?.scrollLeft > 0);
        await page.setViewportSize({ width: 1440, height: 900 });
      });

      await context.close();
    }
  }
} finally {
  if (output) {
    await mkdir(output, { recursive: true });
    await writeFile(path.join(output, 'docs-ux-regression.json'), JSON.stringify(results, null, 2));
  }
  await browser.close();
}

if (results.some(result => !result.passed)) process.exitCode = 1;
