import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/hint-audit';
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true,
    ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const results = [];
try {
    for (const theme of ['admiral', 'admiral-dark']) {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
            { name: '.AspNetCore.Culture', value: 'c=ru|uic=ru', url: baseURL }]);
        const page = await context.newPage();
        page.setDefaultTimeout(7000);
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        const opener = page.getByRole('button', { name: 'Открыть Hint', exact: true });
        const popup = page.locator('.admiral-hint-popup');
        const open = async () => { await opener.click(); await popup.waitFor(); };
        const close = async () => { await page.getByRole('button', { name: 'Закрыть Hint', exact: true }).click(); await popup.waitFor({ state: 'detached' }); };
        const insideViewport = async () => {
            const box = await popup.boundingBox();
            const viewport = page.viewportSize();
            assert.ok(box.x >= 15 && box.y >= 15, JSON.stringify(box));
            assert.ok(box.x + box.width <= viewport.width - 15, JSON.stringify(box));
            assert.ok(box.y + box.height <= viewport.height - 15, JSON.stringify(box));
        };
        const test = async (name, run) => {
            errors.length = 0;
            try {
                await page.setViewportSize({ width: 1440, height: 1000 });
                await page.goto(baseURL + '/components/hint', { waitUntil: 'domcontentloaded' });
                await opener.waitFor();
                await page.waitForTimeout(300);
                await run();
                assert.deepEqual(errors, []);
                assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
                results.push({ theme, name, passed: true });
            } catch (error) {
                results.push({ theme, name, passed: false, error: error.stack, browserErrors: [...errors] });
                await page.screenshot({ path: `${output}/${theme}-${results.length}-failed.png` });
            }
            console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
        };
        await test('Click: Enter/Space, focus trap, Escape restoration and callbacks', async () => {
            await opener.focus();
            await page.keyboard.press('Enter');
            await popup.waitFor();
            await page.waitForFunction(() => !!document.activeElement?.closest('.admiral-hint__dialog'));
            for (const key of ['Shift+Tab', 'Tab', 'Tab', 'Tab']) {
                await page.keyboard.press(key);
                assert.ok(await page.evaluate(() => !!document.activeElement.closest('.admiral-hint__dialog')));
            }
            await page.keyboard.press('Escape');
            await popup.waitFor({ state: 'detached' });
            assert.equal(await opener.evaluate(e => e === document.activeElement), true);
            assert.equal(await opener.getAttribute('aria-expanded'), 'false');
            await page.keyboard.press('Space');
            await popup.waitFor();
            await page.getByRole('button', { name: 'Действие в подсказке', exact: true }).click();
            await page.getByText('Открыто: True · Изменений: 3 · Действий: 1', { exact: true }).waitFor();
            await close();
        });
        await test('Three widths and theme contrast; close hover/focus/press', async () => {
            for (const [size, width] of [['Small', 280], ['Medium', 384], ['Large', 488]]) {
                await page.getByLabel('Размер', { exact: true }).selectOption(size);
                await open();
                assert.equal(Math.round((await popup.boundingBox()).width), width);
                await insideViewport();
                assert.equal(await popup.evaluate(e => getComputedStyle(e).backgroundColor),
                    theme === 'admiral' ? 'rgb(255, 255, 255)' : 'rgb(32, 33, 34)');
                await close();
            }
            await open();
            const button = page.getByRole('button', { name: 'Закрыть Hint', exact: true });
            await page.getByRole('button', { name: 'Действие в подсказке', exact: true }).focus();
            await button.hover();
            assert.equal(await button.evaluate(e => getComputedStyle(e).backgroundColor), 'rgba(0, 0, 0, 0)');
            assert.notEqual(await button.evaluate(e => getComputedStyle(e, '::before').backgroundColor), 'rgba(0, 0, 0, 0)');
            assert.equal(Math.round((await button.boundingBox()).width), 20);
            await button.evaluate(e => e.addEventListener('click', event => {
                event.preventDefault(); event.stopPropagation();
            }, { once: true }));
            await page.mouse.down();
            assert.equal(await button.evaluate(e => getComputedStyle(e, '::before').backgroundColor),
                theme === 'admiral' ? 'rgba(0, 0, 0, 0.12)' : 'rgba(255, 255, 255, 0.12)');
            await page.mouse.up();
            await page.getByRole('button', { name: 'Действие в подсказке', exact: true }).focus();
            await page.keyboard.press('Tab');
            assert.equal(await button.evaluate(e => e.matches(':focus-visible')), true);
            assert.equal(await button.evaluate(e => getComputedStyle(e).outlineWidth), '2px');
            assert.equal(await button.evaluate(e => getComputedStyle(e).borderRadius), '4px');
            assert.equal(await button.evaluate(e => getComputedStyle(e, '::before').backgroundColor), 'rgba(0, 0, 0, 0)');
            await page.screenshot({ path: `${output}/${theme}-desktop.png` });
            await close();
        });
        await test('Four preferred sides and automatic fallback at window corners', async () => {
            for (const side of ['Top', 'Right', 'Bottom', 'Left', 'Auto']) {
                await page.getByLabel('Положение', { exact: true }).selectOption(side);
                await opener.evaluate(e => {
                    const anchor = e.parentElement;
                    anchor.style.cssText = 'position:fixed;left:0;top:0;z-index:100';
                    const origin = anchor.getBoundingClientRect();
                    anchor.style.left = `${700 - origin.x}px`;
                    anchor.style.top = `${480 - origin.y}px`;
                });
                await open();
                await insideViewport();
                assert.equal(await popup.getAttribute('data-placement'), side === 'Auto' ? 'bottom' : side.toLowerCase());
                await close();
            }
            for (const [x, y] of [[16, 16], [1200, 16], [16, 900], [1200, 900]]) {
                await opener.evaluate((e, [x, y]) => {
                    const anchor = e.parentElement;
                    anchor.style.cssText = 'position:fixed;left:0;top:0;z-index:100';
                    const origin = anchor.getBoundingClientRect();
                    anchor.style.left = `${x - origin.x}px`;
                    anchor.style.top = `${y - origin.y}px`;
                }, [x, y]);
                // Edge fixtures can place the trigger behind the smoke sidebar/header.
                await opener.evaluate(e => e.click());
                await popup.waitFor(); await insideViewport(); await close();
            }
        });
        await test('Responsive repositioning while open and 360px mobile width', async () => {
            await page.getByLabel('Положение', { exact: true }).selectOption('Right');
            await open();
            await page.setViewportSize({ width: 360, height: 740 });
            await page.waitForTimeout(200);
            await insideViewport();
            assert.equal(Math.round((await popup.boundingBox()).width), 328);
            assert.ok(['top', 'bottom'].includes(await popup.getAttribute('data-placement')));
            await page.screenshot({ path: `${output}/${theme}-mobile.png` });
            await close();
        });
        await test('Hover: pointer bridge, focus, interactive content and no focus stealing', async () => {
            await page.getByLabel('Триггер', { exact: true }).selectOption('Hover');
            await opener.hover();
            await popup.waitFor();
            assert.equal(await popup.locator('.admiral-hint__close').count(), 0);
            assert.equal(await page.evaluate(() => !!document.activeElement.closest('.admiral-hint-popup')), false);
            await popup.hover();
            await page.waitForTimeout(150);
            assert.equal(await popup.isVisible(), true);
            await page.mouse.move(1400, 990);
            await popup.waitFor({ state: 'detached' });
            await opener.focus();
            await popup.waitFor();
            assert.equal(await opener.evaluate(e => e === document.activeElement), true);
            await page.keyboard.press('Tab');
            assert.equal(await page.getByRole('button', { name: 'Действие в подсказке', exact: true })
                .evaluate(e => e === document.activeElement), true);
            await page.keyboard.press('Shift+Tab');
            assert.equal(await opener.evaluate(e => e === document.activeElement), true);
            await page.keyboard.press('Escape');
            await popup.waitFor({ state: 'detached' });
        });
        await test('Hover closes on resize; click repositions on scroll', async () => {
            await page.getByLabel('Триггер', { exact: true }).selectOption('Hover');
            await opener.hover(); await popup.waitFor();
            await page.setViewportSize({ width: 1280, height: 900 });
            await popup.waitFor({ state: 'detached' });
            await page.mouse.move(1200, 880);
            await page.getByLabel('Триггер', { exact: true }).selectOption('Click');
            await open();
            await page.evaluate(() => window.scrollBy(0, 60));
            await page.waitForTimeout(150);
            assert.equal(await popup.isVisible(), true);
            await insideViewport(); await close();
        });
        await test('Outside-click configuration, focus opt-out and disabled trigger', async () => {
            await page.getByLabel('Не перемещать фокус', { exact: true }).check();
            await page.getByLabel('Закрытие снаружи', { exact: true }).uncheck();
            await open();
            assert.equal(await opener.evaluate(e => e === document.activeElement), true);
            await page.mouse.click(1400, 950);
            assert.equal(await popup.isVisible(), true);
            await page.getByRole('button', { name: 'Действие в подсказке', exact: true }).focus();
            await page.keyboard.press('Escape');
            await popup.waitFor({ state: 'detached' });
            await page.waitForFunction(() => document.activeElement?.textContent.includes('Открыть Hint'));
            await open();
            await close();
            await page.getByLabel('Закрытие снаружи', { exact: true }).check();
            await open(); await page.mouse.click(1400, 950);
            await popup.waitFor({ state: 'detached' });
            await page.getByLabel('Disabled', { exact: true }).check();
            await page.waitForFunction(() => document.querySelector('.admiral-hint-anchor button')?.disabled);
            assert.equal(await opener.isDisabled(), true);
            await opener.hover({ force: true });
            assert.equal(await popup.count(), 0);
        });
        await test('Long content scroll, custom trigger and direct Radzen theme example', async () => {
            await page.getByLabel('Длинное содержимое', { exact: true }).check();
            await open();
            const content = popup.locator('.admiral-hint__content');
            assert.ok(await content.evaluate(e => e.scrollHeight > e.clientHeight));
            await content.evaluate(e => { e.scrollTop = e.scrollHeight; });
            assert.ok(await content.evaluate(e => e.scrollTop > 0));
            await insideViewport(); await close();
            await page.getByRole('button', { name: 'Сведения о записи', exact: true }).click();
            await page.getByRole('dialog', { name: 'Подсказка к иконке', exact: true }).waitFor();
            const custom = page.getByRole('button', { name: 'Сведения о записи', exact: true });
            const anchorBox = await custom.locator('..').boundingBox();
            const triggerBox = await custom.boundingBox();
            const popupBox = await popup.boundingBox();
            assert.ok(Math.abs(anchorBox.width - triggerBox.width) < 1, 'Custom anchor must not stretch across its flex parent');
            assert.ok(popupBox.x <= triggerBox.x + triggerBox.width + 8, 'Popup must remain near its custom trigger');
            assert.equal(await popup.locator('.admiral-hint__content').evaluate(e => getComputedStyle(e).paddingTop), '12px');
            await page.keyboard.press('Escape'); await popup.waitFor({ state: 'detached' });
            await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
            await page.getByRole('button', { name: 'Открыть Radzen Hint', exact: true }).click();
            await page.locator('.rz-tooltip.admiral-hint').waitFor();
            await page.getByRole('button', { name: 'Закрыть Radzen Hint', exact: true }).click();
            await page.locator('.rz-tooltip.admiral-hint').waitFor({ state: 'detached' });
        });
        await context.close();
    }
} finally { await browser.close(); }
await writeFile(`${output}/hint-interactions.json`, JSON.stringify(results, null, 2));
process.exitCode = results.some(result => !result.passed) ? 1 : 0;
