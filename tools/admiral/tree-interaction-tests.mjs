import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const base = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/tree-tests';
const results = [];
await mkdir(output, { recursive: true });
try {
    for (const theme of ['admiral', 'admiral-dark']) {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: base },
            { name: '.AspNetCore.Culture', value: 'c=ru|uic=ru', url: base }]);
        const page = await context.newPage();
        page.setDefaultTimeout(7000);
        const tree = page.getByRole('tree', { name: 'Документы компании', exact: true });
        const row = id => tree.locator(`[data-tree-id="${id}"]`);
        const state = (id, attribute, value) => page.waitForFunction(({ id, attribute, value }) => document.querySelector(`[data-tree-id="${id}"]`)?.getAttribute(attribute) === value, { id, attribute, value });
        const focus = id => page.waitForFunction(id => document.activeElement?.dataset.treeId === id, id);
        const test = async (name, run) => {
            const errors = [];
            const onError = error => errors.push(error.message);
            page.on('pageerror', onError);
            try {
                await page.setViewportSize({ width: 1440, height: 1000 });
                await page.goto(base + '/components/tree');
                await tree.waitFor(); await page.waitForTimeout(150);
                await run();
                assert.deepEqual(errors, []);
                assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
                results.push({ theme, name, passed: true });
            } catch (error) { results.push({ theme, name, passed: false, error: error.stack }); }
            finally { page.off('pageerror', onError); }
            console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
        };
        await test('M/S geometry, hierarchy metadata, icons and wrapping', async () => {
            for (const [size, height, icon, indent] of [['Medium', 40, 24, 40], ['Small', 32, 20, 36]]) {
                await page.locator('.admiral-component-controls select').selectOption(size);
                await page.waitForTimeout(100);
                const dimensions = await row('docs').evaluate(e => ({ height: e.getBoundingClientRect().height, icon: e.querySelector('.admiral-tree-view__icon').getBoundingClientRect().width, padding: parseFloat(getComputedStyle(e).paddingInlineStart) }));
                assert.equal(dimensions.height, height); assert.equal(dimensions.icon, icon);
                const expectedIconColor = theme === 'admiral-dark' ? 'rgb(130, 134, 139)' : 'rgb(116, 118, 123)';
                assert.equal(await row('docs').locator('.admiral-tree-view__icon').evaluate(e => getComputedStyle(e).color), expectedIconColor);
                assert.equal(await row('docs').locator('.admiral-tree-view__expand').evaluate(e => getComputedStyle(e).color), expectedIconColor);
                assert.equal(await row('reports').evaluate(e => parseFloat(getComputedStyle(e).paddingInlineStart)), dimensions.padding + indent);
                assert.equal(await row('month').getAttribute('aria-level'), '3');
                assert.equal(await row('year').getAttribute('aria-posinset'), '2');
                assert.equal(await row('year').getAttribute('aria-setsize'), '2');
            }
            await page.setViewportSize({ width: 700, height: 900 });
            assert.ok(await tree.evaluate(e => e.scrollWidth <= e.clientWidth + 1));
        });
        await test('cascade checks descendants and parents; disabled branches excluded', async () => {
            await row('docs').click();
            for (const id of ['docs', 'reports', 'month', 'year', 'notes']) await state(id, 'aria-checked', 'true');
            for (const id of ['archive', 'locked', 'locked-child']) await state(id, 'aria-checked', 'false');
            await row('year').click();
            await state('year', 'aria-checked', 'false'); await state('reports', 'aria-checked', 'mixed'); await state('docs', 'aria-checked', 'mixed');
            await row('reports').locator('.admiral-tree-view__checkbox').click();
            await state('reports', 'aria-checked', 'true'); await state('year', 'aria-checked', 'true');
        });
        await test('strict mode checks only the selected node', async () => {
            await page.getByLabel('CheckStrictly', { exact: true }).check();
            await row('reports').click(); await state('reports', 'aria-checked', 'true');
            await state('month', 'aria-checked', 'false'); await state('docs', 'aria-checked', 'false');
            await row('reports').click(); await state('reports', 'aria-checked', 'false');
        });
        await test('single selection, keyboard parent/child navigation and selected-child collapse', async () => {
            await page.getByLabel('AllowCheckBoxes', { exact: true }).uncheck();
            await row('month').click(); await state('month', 'aria-selected', 'true'); await focus('month');
            await page.keyboard.press('ArrowLeft'); await focus('reports');
            await page.keyboard.press('ArrowLeft'); await state('reports', 'aria-expanded', 'false');
            assert.equal(await row('month').count(), 0);
            await page.keyboard.press('ArrowRight'); await state('reports', 'aria-expanded', 'true');
            await state('month', 'aria-selected', 'true');
            await page.keyboard.press('ArrowRight'); await focus('month');
            await page.keyboard.press('ArrowDown'); await focus('year');
            await page.keyboard.press('ArrowDown'); await focus('notes');
            await page.keyboard.press('End'); await focus('settings');
            await page.keyboard.press('Home'); await focus('docs');
            await page.keyboard.press('ArrowDown'); await focus('reports');
            await page.keyboard.press('Space'); await state('reports', 'aria-selected', 'true');
            assert.equal(await tree.locator('.rz-chkbox').count(), 0);
        });
        await test('Enter/Space, letter navigation, Tab exit and no page scroll on arrows', async () => {
            await row('docs').focus();
            const scrollY = await page.evaluate(() => scrollY);
            await page.keyboard.press('ArrowDown'); await focus('reports');
            assert.equal(await page.evaluate(() => scrollY), scrollY);
            await page.keyboard.press('Enter'); await state('reports', 'aria-checked', 'true');
            await page.keyboard.press('Space'); await state('reports', 'aria-checked', 'false');
            await row('reports').dispatchEvent('keydown', { key: 'н', code: 'KeyY', bubbles: true }); await focus('settings');
            await page.keyboard.press('Tab');
            assert.equal(await tree.evaluate(e => e.contains(document.activeElement)), false);
        });
        await test('disabled node and inherited-disabled subtree cannot mutate or collapse', async () => {
            await row('archive').click({ force: true }); await state('archive', 'aria-selected', 'false'); await state('archive', 'aria-checked', 'false');
            await row('locked-child').click({ force: true }); await state('locked-child', 'aria-disabled', 'true'); await state('locked-child', 'aria-selected', 'false');
            await row('locked').locator('button').dispatchEvent('click'); await state('locked', 'aria-expanded', 'true');
            await page.getByLabel('Disabled', { exact: true }).check();
            await state('docs', 'aria-disabled', 'true');
            await row('docs').dispatchEvent('click'); await state('docs', 'aria-checked', 'false');
            await row('docs').focus(); await page.keyboard.press('Space'); await state('docs', 'aria-checked', 'false');
        });
        await test('expansion alone never checks/selects and external reset is applied', async () => {
            await row('reports').locator('button').click(); await state('reports', 'aria-expanded', 'false');
            await state('reports', 'aria-selected', 'false'); await state('reports', 'aria-checked', 'false');
            await row('reports').locator('button').click(); await state('reports', 'aria-expanded', 'true');
            await row('month').click(); await state('month', 'aria-checked', 'true');
            await page.getByRole('button', { name: 'Сбросить', exact: true }).click();
            await state('month', 'aria-checked', 'false'); await state('month', 'aria-selected', 'false');
        });
        await test('hover/focus/hover+focus/active/selected and disabled colors', async () => {
            const target = row('reports');
            await page.mouse.move(0, 0);
            const normal = await target.evaluate(e => getComputedStyle(e).backgroundColor);
            await target.hover(); const hover = await target.evaluate(e => getComputedStyle(e).backgroundColor);
            assert.notEqual(hover, normal);
            await page.keyboard.press('Tab'); await target.focus();
            assert.equal(await target.evaluate(e => getComputedStyle(e).outlineStyle), 'solid');
            await page.mouse.down(); const active = await target.evaluate(e => getComputedStyle(e).backgroundColor); await page.mouse.up();
            assert.notEqual(active, hover); await state('reports', 'aria-selected', 'true');
            await page.mouse.move(0, 0); const selected = await target.evaluate(e => getComputedStyle(e).backgroundColor);
            assert.notEqual(selected, normal);
            const disabled = row('archive'); const color = await disabled.evaluate(e => getComputedStyle(e).color);
            assert.notEqual(color, await target.evaluate(e => getComputedStyle(e).color));
            await page.waitForTimeout(350);
            await page.screenshot({ path: `${output}/tree-${theme}.png`, fullPage: true });
        });
        await test('template, empty state and accessible root survive parameter changes', async () => {
            await page.getByLabel('ItemTemplate', { exact: true }).check();
            await tree.locator('small').first().waitFor();
            await page.getByLabel('Пустое состояние', { exact: true }).check();
            await tree.getByText('Нет элементов', { exact: true }).waitFor();
            assert.equal(await tree.getAttribute('tabindex'), '0');
            await page.getByLabel('Пустое состояние', { exact: true }).uncheck(); await row('docs').waitFor();
            assert.equal(await tree.locator('[role="treeitem"][tabindex="0"]').count(), 1);
        });
        await test('API and independent direct Radzen tree remain runnable', async () => {
            await page.locator('.component-doc-tabs [data-doc-tab="api"]').click();
            await page.getByRole('table', { name: 'Состояния и модель', exact: true }).waitFor();
            await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
            const direct = page.locator('.rz-tree.admiral-tree'); await direct.waitFor();
            await page.getByLabel('AllowCheckBoxes', { exact: true }).check();
            await direct.locator('.rz-chkbox').first().waitFor();
            await page.getByLabel('Маленькое', { exact: true }).check();
            await page.waitForFunction(() => !!document.querySelector('.rz-tree.admiral-tree--small'));
        });
        await test('uncontrolled state survives unrelated parent renders', async () => {
            const local = page.getByRole('tree', { name: 'Локальное состояние', exact: true });
            const localRow = id => local.locator(`[data-tree-id="${id}"]`);
            await localRow('month').click();
            await page.waitForFunction(() => document.querySelector('[aria-label="Локальное состояние"] [data-tree-id="month"]')?.getAttribute('aria-checked') === 'true');
            await localRow('reports').locator('button').click();
            await page.waitForFunction(() => document.querySelector('[aria-label="Локальное состояние"] [data-tree-id="reports"]')?.getAttribute('aria-expanded') === 'false');
            await page.getByLabel('ItemTemplate', { exact: true }).check();
            assert.equal(await localRow('reports').getAttribute('aria-expanded'), 'false');
            await localRow('reports').locator('button').click();
            await localRow('month').waitFor();
            assert.equal(await localRow('month').getAttribute('aria-checked'), 'true');
            assert.equal(await localRow('month').getAttribute('aria-selected'), 'true');
        });
        await test('IME composition and modifier shortcuts do not mutate selection', async () => {
            await row('docs').focus();
            await row('docs').dispatchEvent('keydown', { key: 'Enter', code: 'Enter', isComposing: true, bubbles: true });
            await state('docs', 'aria-checked', 'false');
            await page.keyboard.press('Control+ArrowDown'); await focus('docs');
            await page.keyboard.press('Meta+ArrowDown'); await focus('docs');
            await page.keyboard.press('Enter'); await state('docs', 'aria-checked', 'true');
        });
        await context.close();
    }
} finally {
    await writeFile(`${output}/tree-interactions.json`, JSON.stringify(results, null, 2));
    await browser.close();
}
if (results.some(result => !result.passed)) process.exitCode = 1;
