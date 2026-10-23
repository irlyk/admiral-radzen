import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const base = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/action-bar-tests';
const results = [];
await mkdir(output, { recursive: true });
try {
    for (const theme of ['admiral', 'admiral-dark']) {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: base },
            { name: '.AspNetCore.Culture', value: 'c=ru|uic=ru', url: base }]);
        const page = await context.newPage();
        page.setDefaultTimeout(7000);
        const bar = page.getByRole('toolbar', { name: 'Действия с документом', exact: true });
        const buttons = bar.locator(':scope > button');
        const more = bar.getByRole('button', { name: 'Ещё действия', exact: true });
        const menuTexts = () => page.locator('[role="menuitem"] .admiral-menu-item__text').allTextContents();
        const menuReady = async () => {
            await page.getByRole('menu').waitFor();
            await page.waitForFunction(() => document.activeElement?.closest('[role="menu"]'));
        };
        const open = async () => {
            await page.setViewportSize({ width: 1440, height: 1000 });
            await page.goto(base + '/components/action-bar');
            await bar.waitFor();
            await page.waitForTimeout(200);
        };
        const width = async value => {
            await page.locator('.action-bar-demo-host').evaluate((element, value) => element.style.width = value + 'px', value);
        };
        const count = async value => {
            await page.waitForFunction(value => document.querySelectorAll('.admiral-action-bar--adaptive > button').length === value, value);
        };
        const test = async (name, run) => {
            const errors = [];
            const onError = error => errors.push(error.message);
            page.on('pageerror', onError);
            try {
                await open();
                await run();
                assert.deepEqual(errors, []);
                assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
                results.push({ theme, name, passed: true });
            } catch (error) { results.push({ theme, name, passed: false, error: error.stack }); }
            finally { page.off('pageerror', onError); }
            console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
        };
        await test('suffix overflow, exact fit, only-menu width, restore without duplicates', async () => {
            await count(5); assert.equal(await more.count(), 0);
            await width(200); await count(2);
            await more.click();
            await menuReady();
            assert.deepEqual(await menuTexts(), ['Копировать', 'Скачать', 'Удалить']);
            await page.screenshot({ path: `${output}/action-bar-overflow-${theme}.png`, fullPage: true });
            await page.keyboard.press('Escape');
            await width(64); await count(0); assert.equal(await more.count(), 1);
            await width(320); await count(5); assert.equal(await more.count(), 0);
            assert.equal(await bar.locator('.admiral-action-bar__divider').count(), 2);
        });
        await test('menu callbacks, disabled exclusion, native Space/Enter and Escape restoration', async () => {
            await width(200); await count(2);
            await more.focus(); await page.keyboard.press('ArrowDown');
            await menuReady();
            await page.getByRole('menuitem', { name: 'Копировать', exact: true }).waitFor();
            assert.equal(await page.getByRole('menuitem', { name: 'Удалить', exact: true }).isDisabled(), true);
            await page.keyboard.press('End');
            assert.equal(await page.getByRole('menuitem', { name: 'Скачать', exact: true }).evaluate(e => e === document.activeElement), true);
            await page.keyboard.press('Home'); await page.keyboard.press('Enter');
            await page.waitForFunction(() => document.querySelector('.action-bar-demo-result').textContent.includes('copy'));
            await page.waitForFunction(() => !document.querySelector('[role="menu"]'));
            await more.focus(); await page.keyboard.press('Space');
            await menuReady(); await page.keyboard.press('Escape');
            await page.waitForFunction(() => document.activeElement?.getAttribute('aria-label') === 'Ещё действия');
            assert.equal(await more.evaluate(e => e === document.activeElement), true);
            await buttons.first().focus(); await page.keyboard.press('Enter');
            await page.waitForFunction(() => document.querySelector('.action-bar-demo-result').textContent.includes('search'));
        });
        await test('focus survives moving selected action into overflow and back', async () => {
            await buttons.nth(3).focus();
            await width(200); await count(2);
            await page.waitForFunction(() => document.activeElement?.getAttribute('aria-label') === 'Ещё действия');
            await width(400); await count(5);
            await page.waitForFunction(() => document.activeElement?.getAttribute('aria-label') === 'Найти');
        });
        await test('manual overflow and changed Items recompute without losing order', async () => {
            await page.getByLabel('Постоянные действия в меню', { exact: true }).check();
            await more.click();
            await menuReady();
            assert.deepEqual(await menuTexts(), ['Настройки документа']);
            await page.keyboard.press('Escape');
            await width(200); await count(2); await more.click();
            await menuReady();
            assert.deepEqual(await menuTexts(), ['Копировать', 'Скачать', 'Удалить', 'Настройки документа']);
            await page.keyboard.press('Escape');
            await page.getByLabel('Сократить Items до двух', { exact: true }).check();
            await more.click();
            await menuReady();
            assert.deepEqual(await menuTexts(), ['Настройки документа']);
            await page.keyboard.press('Escape');
            await page.getByLabel('AutoOverflow', { exact: true }).uncheck();
            await page.getByLabel('Сократить Items до двух', { exact: true }).uncheck();
            await page.waitForFunction(() => document.querySelectorAll('.action-bar-demo-host > .admiral-action-bar > button').length === 5);
            assert.equal(await buttons.count(), 5);
        });
        await test('S/M/L/XL geometry, icon centering and divider widths', async () => {
            await page.getByLabel('Постоянные действия в меню', { exact: true }).check();
            for (const [size, dimension, icon] of [['Small', 32, 20], ['Medium', 40, 24], ['Large', 48, 24], ['ExtraLarge', 56, 24]]) {
                await page.locator('.admiral-component-controls select').first().selectOption(size);
                await page.waitForTimeout(120);
                const geometry = await buttons.first().evaluate(e => {
                    const rect = e.getBoundingClientRect(), icon = e.querySelector('.admiral-icon').getBoundingClientRect();
                    return { width: rect.width, height: rect.height, icon: icon.width, dx: icon.x + icon.width / 2 - rect.x - rect.width / 2, dy: icon.y + icon.height / 2 - rect.y - rect.height / 2 };
                });
                assert.equal(geometry.width, dimension); assert.equal(geometry.height, dimension); assert.equal(geometry.icon, icon);
                assert.ok(Math.abs(geometry.dx) < 1 && Math.abs(geometry.dy) < 1);
                assert.equal(await bar.locator('.admiral-action-bar__divider').first().evaluate(e => e.getBoundingClientRect().width), size === 'Small' ? 16 : 20);
                await more.click(); await menuReady();
                const menuGeometry = await page.getByRole('menuitem').first().evaluate(e => ({ height: e.getBoundingClientRect().height, icon: e.querySelector('.admiral-icon').getBoundingClientRect().width }));
                assert.equal(menuGeometry.height, Math.min(dimension, 48));
                assert.equal(menuGeometry.icon, icon);
                await page.keyboard.press('Escape');
            }
        });
        await test('default/hover/focus/hover+focus/active/disabled reuse IconButton states', async () => {
            const button = buttons.first();
            const style = () => button.evaluate(e => { const s = getComputedStyle(e); return { color: s.color, background: s.backgroundColor, outline: s.outlineStyle, radius: s.borderRadius }; });
            await page.mouse.move(0, 0); const normal = await style();
            await button.hover(); const hover = await style();
            assert.notEqual(hover.background, normal.background);
            await page.keyboard.press('Tab'); await button.focus();
            assert.equal((await style()).outline, 'solid');
            assert.equal((await style()).background, hover.background);
            await page.mouse.down(); const active = await style(); await page.mouse.up();
            assert.notEqual(active.background, hover.background);
            await page.getByLabel('Disabled', { exact: true }).check();
            await page.waitForFunction(() => document.querySelector('.admiral-action-bar--adaptive > button')?.disabled);
            assert.equal(await button.isDisabled(), true);
            assert.notEqual((await style()).color, normal.color);
            await page.screenshot({ path: `${output}/action-bar-${theme}.png`, fullPage: true });
        });
        await test('resize closes stale popup and real viewport resize recomputes width', async () => {
            await width(200); await count(2); await more.click();
            await width(64); await count(0);
            await page.waitForFunction(() => !document.querySelector('[role="menu"]'));
            await width(640); await count(5);
            await page.setViewportSize({ width: 360, height: 900 });
            await page.waitForFunction(() => document.querySelectorAll('.admiral-action-bar--adaptive > button').length < 5);
            const bounds = await bar.boundingBox();
            for (const button of await bar.locator('.admiral-action-bar__item').all()) {
                const child = await button.boundingBox();
                assert.ok(child.x + child.width <= bounds.x + bounds.width + 1);
            }
            await page.screenshot({ path: `${output}/action-bar-mobile-${theme}.png`, fullPage: true });
            await page.setViewportSize({ width: 1440, height: 1000 });
            await count(5);
        });
        await test('GroupActionsPane search Escape, columns and settings remain functional', async () => {
            await page.getByRole('button', { name: 'Открыть поиск', exact: true }).click();
            const search = page.getByRole('textbox', { name: 'Поиск', exact: true });
            await search.fill('документ'); await page.keyboard.press('Escape');
            await search.waitFor({ state: 'detached' });
            await page.getByRole('button', { name: 'Выбрать колонки', exact: true }).click();
            const popup = page.getByRole('dialog', { name: 'Колонки', exact: true });
            await popup.waitFor();
            await popup.locator('label').filter({ hasText: 'Комментарий' }).click();
            await page.waitForFunction(() => [...document.querySelectorAll('[role="dialog"] label')].find(e => e.textContent.includes('Комментарий'))?.querySelector('[role="checkbox"]')?.getAttribute('aria-checked') === 'true');
            await page.getByRole('button', { name: 'Настройки', exact: true }).click();
            await page.waitForFunction(() => document.querySelector('.action-bar-demo-result').textContent.includes('Открыть настройки'));
        });
        await test('table GroupActionsPane actions keep focus ring and hover contrast', async () => {
            await page.goto(base + '/tables');
            const panes = page.locator('.admiral-group-actions-pane');
            await panes.first().waitFor();
            await page.waitForTimeout(250);
            for (const pane of await panes.all()) {
                const action = pane.locator('.admiral-group-actions-pane__actions > button').first();
                if (!await action.count() || !await action.isVisible()) continue;
                await action.focus();
                const focus = await action.evaluate(element => ({
                    visible: element.matches(':focus-visible'),
                    offset: parseFloat(getComputedStyle(element).outlineOffset),
                    gap: element.getBoundingClientRect().left - element.closest('.admiral-group-actions-pane').getBoundingClientRect().left,
                    height: element.getBoundingClientRect().height,
                    paneHeight: element.closest('.admiral-group-actions-pane').getBoundingClientRect().height
                }));
                assert.ok(focus.visible && focus.offset === 2 && Math.abs(focus.gap) < 1 && focus.height === focus.paneHeight,
                    `text action differs from the reference pane: ${JSON.stringify(focus)}`);
                const clipping = await action.evaluate(element => {
                    const pane = element.closest('.admiral-group-actions-pane');
                    const viewport = pane.closest('.admiral-table-gallery__scroll');
                    const paneRect = pane.getBoundingClientRect();
                    const viewportRect = viewport?.getBoundingClientRect();
                    return {
                        zIndex: Number(getComputedStyle(pane).zIndex),
                        room: viewportRect && {
                            left: paneRect.left - viewportRect.left,
                            top: paneRect.top - viewportRect.top,
                            right: viewportRect.right - paneRect.right
                        }
                    };
                });
                assert.ok(clipping.zIndex >= 3, `focused pane is behind the grid header: ${JSON.stringify(clipping)}`);
                if (clipping.room) {
                    assert.ok(Object.values(clipping.room).every(space => space >= 4),
                        `scroll viewport clips the 4px focus ring: ${JSON.stringify(clipping)}`);
                }
                const tool = pane.locator('.admiral-group-actions-pane__tools > button').first();
                if (await tool.count()) {
                    await page.keyboard.press('Tab');
                    await tool.focus();
                    assert.equal(await tool.evaluate(element => element.matches(':focus-visible')), true);
                    assert.equal(await tool.evaluate(element => parseFloat(getComputedStyle(element).outlineOffset)), focus.offset,
                        'text and icon actions use the same focus offset');
                }
                const baseColor = await action.evaluate(element => getComputedStyle(element).color);
                await action.hover();
                assert.notEqual(await action.evaluate(element => getComputedStyle(element).color), baseColor);
            }
        });
        await test('API and direct theme tabs are runnable independently', async () => {
            await page.locator('.component-doc-tabs [data-doc-tab="api"]').click();
            await page.getByRole('table', { name: 'AdmiralActionBar', exact: true }).waitFor();
            await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
            await page.getByRole('toolbar', { name: 'Прямой Radzen', exact: true }).getByRole('button', { name: 'Найти', exact: true }).click();
            await page.getByText('Radzen: найти', { exact: true }).waitFor();
        });
        await context.close();
    }
} finally {
    await writeFile(`${output}/action-bar-interactions.json`, JSON.stringify(results, null, 2));
    await browser.close();
}
if (results.some(result => !result.passed)) process.exitCode = 1;
