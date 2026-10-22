import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/drawer-audit';
const browser = await chromium.launch({ headless: true,
    ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const results = [];
await mkdir(output, { recursive: true });

try {
    for (const theme of ['admiral', 'admiral-dark']) {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        await context.addCookies([
            { name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
            { name: '.AspNetCore.Culture', value: 'c=ru|uic=ru', url: baseURL }
        ]);
        const page = await context.newPage();
        page.setDefaultTimeout(7000);
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        const drawer = page.locator('.admiral-drawer');
        const backdrop = page.locator('.admiral-drawer-backdrop');
        const opener = page.getByRole('button', { name: 'Открыть Drawer', exact: true });
        const open = async () => {
            await opener.click();
            await drawer.waitFor();
            await page.waitForFunction(() => {
                const root = document.querySelector('.admiral-drawer');
                return root && root.getAnimations().every(animation => animation.playState === 'finished');
            });
        };
        const close = async () => {
            await drawer.locator('.admiral-drawer__close').click();
            await drawer.waitFor({ state: 'detached' });
        };
        const test = async (name, run) => {
            errors.length = 0;
            try {
                await page.setViewportSize({ width: 1440, height: 1000 });
                await page.emulateMedia({ reducedMotion: 'no-preference' });
                await page.goto(baseURL + '/components/drawer', { waitUntil: 'domcontentloaded' });
                await opener.waitFor();
                // Wait for the Blazor Server circuit before interacting with native controls.
                await page.waitForTimeout(250);
                await run();
                assert.deepEqual(errors, []);
                assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
                results.push({ theme, name, passed: true });
            } catch (error) {
                results.push({ theme, name, passed: false, error: error.message });
                await page.screenshot({ path: `${output}/${theme}-${results.length}-failed.png` });
            }
            console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
        };

        await test('Right/left geometry, width, neutral appearance and close icon states', async () => {
            await open();
            let bounds = await drawer.boundingBox();
            const backdropBounds = await backdrop.boundingBox();
            // The docs shell may reserve a root scrollbar gutter; the drawer must align with its overlay.
            assert.equal(Math.round(bounds.x + bounds.width), Math.round(backdropBounds.x + backdropBounds.width));
            assert.equal(Math.round(bounds.width), 480);
            const primary = await drawer.evaluate(e => getComputedStyle(e).backgroundColor);
            const button = drawer.locator('.admiral-drawer__close');
            assert.equal(Math.round((await button.boundingBox()).width), 24);
            assert.equal(Math.round((await button.boundingBox()).height), 24);
            await button.hover();
            assert.equal(await button.evaluate(e => getComputedStyle(e).backgroundColor), 'rgba(0, 0, 0, 0)');
            assert.notEqual(await button.evaluate(e => getComputedStyle(e, '::before').backgroundColor), 'rgba(0, 0, 0, 0)');
            // Inspect pressed styling without letting this diagnostic click close the panel.
            await button.evaluate(e => e.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
            }, { once: true }));
            await page.mouse.down();
            assert.equal(await button.evaluate(e => getComputedStyle(e, '::before').backgroundColor),
                theme === 'admiral' ? 'rgba(0, 0, 0, 0.12)' : 'rgba(255, 255, 255, 0.12)');
            await page.mouse.up();
            await button.focus();
            await page.keyboard.press('Tab');
            await page.keyboard.press('Shift+Tab');
            assert.equal(await button.evaluate(e => e === document.activeElement), true);
            assert.ok(await button.evaluate(e => {
                const css = getComputedStyle(e);
                return css.outlineStyle !== 'none' || css.boxShadow !== 'none';
            }));
            await button.hover();
            assert.equal(await button.evaluate(e => getComputedStyle(e, '::before').backgroundColor), 'rgba(0, 0, 0, 0)');
            assert.equal(await button.evaluate(e => getComputedStyle(e).borderRadius), '4px');
            await close();
            await page.getByLabel('Сторона', { exact: true }).selectOption('Left');
            await page.getByLabel('Поверхность', { exact: true }).selectOption('Secondary');
            await page.getByLabel('Ширина', { exact: true }).selectOption('40rem');
            await open();
            bounds = await drawer.boundingBox();
            assert.equal(Math.round(bounds.x), 0);
            assert.equal(Math.round(bounds.width), 640);
            assert.notEqual(await drawer.evaluate(e => getComputedStyle(e).backgroundColor), primary);
            assert.equal(await drawer.evaluate(e => getComputedStyle(e).backgroundColor),
                theme === 'admiral' ? 'rgb(245, 245, 246)' : 'rgb(31, 32, 36)');
            await page.screenshot({ path: `${output}/${theme}-left.png` });
            await close();
        });

        await test('Focus containment, Escape restore, callbacks and panel clicks', async () => {
            await open();
            const button = drawer.locator('.admiral-drawer__close');
            assert.equal(await button.evaluate(e => e === document.activeElement), true);
            await page.keyboard.press('Shift+Tab');
            assert.equal(await page.evaluate(() => document.activeElement.textContent.trim()), 'Отмена');
            await page.keyboard.press('Tab');
            assert.equal(await button.evaluate(e => e === document.activeElement), true);
            await drawer.locator('input').click();
            assert.equal(await drawer.count(), 1);
            await page.keyboard.press('Escape');
            await drawer.waitFor({ state: 'detached' });
            assert.equal(await opener.evaluate(e => e === document.activeElement), true);
            await page.getByText('Запросов закрытия: 1 · Пользователь запросил закрытие', { exact: true }).waitFor();
            await open();
            await drawer.getByRole('button', { name: 'Сохранить', exact: true }).click();
            await drawer.waitFor({ state: 'detached' });
            await page.getByText('Запросов закрытия: 1 · Сохранено: Тестовая запись', { exact: true }).waitFor();
        });

        await test('Modal backdrop closes; body locked; content scroll leaves actions in place', async () => {
            await page.getByLabel('Длинное содержимое', { exact: true }).check();
            await open();
            assert.equal(await page.evaluate(() => getComputedStyle(document.body).overflowY), 'hidden');
            assert.equal(await drawer.getAttribute('aria-modal'), 'true');
            const actions = drawer.locator('.admiral-drawer__actions');
            const before = await actions.boundingBox();
            await drawer.locator('.admiral-drawer__content').evaluate(e => { e.scrollTop = e.scrollHeight; });
            assert.ok(await drawer.locator('.admiral-drawer__content').evaluate(e => e.scrollTop > 0));
            assert.equal(Math.round((await actions.boundingBox()).y), Math.round(before.y));
            await page.mouse.click(10, 10);
            await drawer.waitFor({ state: 'detached' });
            assert.notEqual(await page.evaluate(() => getComputedStyle(document.body).overflowY), 'hidden');
        });

        await test('Without backdrop the page remains usable; Escape outside does not close', async () => {
            await page.getByLabel('Затемнение', { exact: true }).uncheck();
            await open();
            assert.equal(await drawer.getAttribute('aria-modal'), 'false');
            assert.equal(await page.locator('.admiral-drawer-backdrop').evaluate(e => getComputedStyle(e).backgroundColor), 'rgba(0, 0, 0, 0)');
            assert.notEqual(await page.evaluate(() => getComputedStyle(document.body).overflowY), 'hidden');
            await page.getByRole('button', { name: 'Проверить доступность фоновой страницы', exact: true }).click();
            await page.getByText('Нажатий на фоновую страницу: 1', { exact: true }).waitFor();
            await page.keyboard.press('Escape');
            assert.equal(await drawer.count(), 1);
            await drawer.locator('.admiral-drawer__close').focus();
            await page.keyboard.press('Escape');
            await drawer.waitFor({ state: 'detached' });
        });

        await test('Non-closable configuration keeps explicit action available', async () => {
            for (const label of ['Кнопка закрытия', 'Закрытие по Escape', 'Закрытие по подложке']) {
                await page.getByLabel(label, { exact: true }).uncheck();
            }
            await open();
            assert.equal(await drawer.locator('.admiral-drawer__close').count(), 0);
            assert.equal(await drawer.locator('input').evaluate(e => e === document.activeElement), true);
            await page.keyboard.press('Escape');
            await page.mouse.click(10, 10);
            assert.equal(await drawer.count(), 1);
            await drawer.getByRole('button', { name: 'Отмена', exact: true }).click();
            await drawer.waitFor({ state: 'detached' });
        });

        await test('Mobile forces right edge; narrow layout fits viewport', async () => {
            await page.getByLabel('Сторона', { exact: true }).selectOption('Left');
            await page.getByLabel('Мобильная композиция', { exact: true }).check();
            await open();
            assert.equal(await drawer.evaluate(e => e.classList.contains('admiral-drawer--right')), true);
            assert.equal(Math.round((await drawer.boundingBox()).x), 16);
            await page.setViewportSize({ width: 360, height: 740 });
            const bounds = await drawer.boundingBox();
            const backdropBounds = await backdrop.boundingBox();
            assert.equal(Math.round(bounds.width), Math.round(backdropBounds.width) - 16);
            assert.equal(Math.round(bounds.height), 740);
            assert.equal(await drawer.locator('.admiral-drawer__actions').evaluate(e => getComputedStyle(e).flexDirection), 'column-reverse');
            await page.screenshot({ path: `${output}/${theme}-mobile.png` });
            await close();
        });

        await test('PreventFocusRestore respects opt-out', async () => {
            await page.getByLabel('Не возвращать фокус', { exact: true }).check();
            await open();
            await close();
            assert.equal(await opener.evaluate(e => e === document.activeElement), false);
        });

        await test('Exit animation is inert; rapid reopening cancels pending removal', async () => {
            await open();
            await drawer.locator('.admiral-drawer__close').click();
            await page.locator('.admiral-drawer--closing').waitFor();
            assert.equal(await drawer.evaluate(e => e.inert), true);
            assert.match(await drawer.evaluate(e => getComputedStyle(e).animationName), /drawer-out-end/);
            await opener.click();
            await page.waitForTimeout(450);
            assert.equal(await drawer.count(), 1);
            assert.equal(await drawer.evaluate(e => e.inert), false);
            await close();
        });

        await test('Reduced motion and independent Radzen example', async () => {
            await page.emulateMedia({ reducedMotion: 'reduce' });
            await open();
            assert.equal(await drawer.evaluate(e => getComputedStyle(e).animationName), 'none');
            await close();
            await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
            await page.getByLabel('Сторона Radzen', { exact: true }).selectOption('Left');
            await page.getByRole('button', { name: 'Открыть Radzen-панель', exact: true }).click();
            await drawer.waitFor();
            assert.equal(await drawer.evaluate(e => e.classList.contains('rz-sidebar')), true);
            assert.equal(Math.round((await drawer.boundingBox()).x), 0);
            await page.getByRole('button', { name: 'Закрыть Radzen-панель', exact: true }).click();
            await drawer.waitFor({ state: 'detached' });
        });
        await context.close();
    }
} finally {
    await browser.close();
    await writeFile(`${output}/drawer-interactions.json`, JSON.stringify(results, null, 2));
}
if (results.some(result => !result.passed)) process.exitCode = 1;
