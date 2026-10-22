import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/slider-audit';
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
        page.setDefaultTimeout(6000);
        const errors = [];
        page.on('pageerror', e => errors.push(e.message));
        const slider = page.locator('.admiral-slider').first();
        const handles = slider.getByRole('slider');
        const check = (name, on = true) => page.getByRole('checkbox', { name, exact: true }).setChecked(on);
        const valueIs = async (handle, value) => {
            await page.waitForFunction(([name, value]) => Number(document.querySelector(`[role=slider][aria-label="${name}"]`)?.getAttribute('aria-valuenow')) === value, [await handle.getAttribute('aria-label'), value]);
        };
        const test = async (name, run) => {
            errors.length = 0;
            try {
                await page.setViewportSize({ width: 1440, height: 1000 });
                await page.goto(baseURL + '/components/slider');
                await handles.first().waitFor();
                await page.waitForTimeout(300);
                await run();
                assert.deepEqual(errors, []);
                assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
                results.push({ theme, name, passed: true });
            } catch (error) {
                results.push({ theme, name, passed: false, error: error.stack });
                await page.screenshot({ path: `${output}/${theme}-${results.length}-failed.png` });
            }
            console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
        };
        await test('Fractional arrows, Home/End, callbacks and ARIA', async () => {
            const h = handles.first();
            await h.focus();
            await page.keyboard.press('ArrowRight'); await valueIs(h, 5.5);
            await page.keyboard.press('ArrowDown'); await valueIs(h, 5.25);
            await page.keyboard.press('Home'); await valueIs(h, 0);
            await page.keyboard.press('End'); await valueIs(h, 10);
            assert.equal(await h.getAttribute('aria-valuemin'), '0');
            assert.equal(await h.getAttribute('aria-valuemax'), '10');
            await page.getByText(/Изменений: 4/).waitFor();
        });
        await test('Range values, independent labels, crossing and focus role exchange', async () => {
            await check('Range');
            await page.getByRole('slider', { name: 'Нижняя граница' }).waitFor();
            const low = handles.first(), high = handles.nth(1);
            await low.focus(); await page.keyboard.press('ArrowRight'); await valueIs(low, 2.5);
            await page.keyboard.press('Tab');
            assert.equal(await high.evaluate(e => e === document.activeElement), true);
            await page.keyboard.press('ArrowLeft'); await valueIs(high, 7.5);
            await low.focus(); await page.keyboard.press('End'); await valueIs(low, 7.5); await valueIs(high, 10);
            assert.equal(await high.evaluate(e => e === document.activeElement), true);
            assert.equal(Number(await low.getAttribute('aria-valuemax')), 10);
            await high.focus(); await page.keyboard.press('Home'); await valueIs(low, 0); await valueIs(high, 7.5);
            assert.equal(await low.evaluate(e => e === document.activeElement), true);
        });
        await test('Localized marks, custom template and nearest range endpoint', async () => {
            await slider.getByRole('button', { name: '2,5', exact: true }).click();
            await valueIs(handles.first(), 2.5);
            await page.getByLabel('Локаль отметок').selectOption('en-US');
            await slider.getByRole('button', { name: '2.5', exact: true }).waitFor();
            await check('MarkTemplate');
            await slider.getByRole('button', { name: '2.5 ₽', exact: true }).waitFor();
            const markLayout = await slider.locator('.admiral-slider__mark').evaluateAll(elements => elements.map(element => ({
                height: element.getBoundingClientRect().height,
                lineHeight: Number.parseFloat(getComputedStyle(element).lineHeight),
                whiteSpace: getComputedStyle(element).whiteSpace
            })));
            assert.ok(markLayout.every(mark => mark.whiteSpace === 'nowrap' && mark.height <= mark.lineHeight),
                'Custom mark labels, including the final currency suffix, must stay on one line');
            const endpointsContained = () => slider.locator('.admiral-slider__marks').evaluate(element => {
                const track = element.getBoundingClientRect();
                const first = element.firstElementChild.getBoundingClientRect();
                const last = element.lastElementChild.getBoundingClientRect();
                return Math.abs(first.left - track.left) < 1 &&
                    Math.abs(last.right - track.right) < 1;
            });
            assert.ok(await endpointsContained(), 'Currency endpoints must stay inside the track bounds');
            await check('MarkTemplate', false);
            assert.ok(await endpointsContained(), 'Numeric endpoints must use the same alignment');
            await check('MarkTemplate');
            await check('Range');
            await slider.getByRole('button', { name: '0 ₽', exact: true }).click();
            await valueIs(handles.first(), 0);
            await slider.getByRole('button', { name: '10 ₽', exact: true }).click();
            await valueIs(handles.nth(1), 10);
        });
        await test('Geometry, hover, focus and nonpersistent range tooltip', async () => {
            const h = handles.first();
            assert.equal(Math.round((await h.boundingBox()).width), 20);
            await page.mouse.move(0, 0);
            assert.equal(await h.evaluate(e => getComputedStyle(e, '::before').opacity), '0');
            const normal = await h.evaluate(e => getComputedStyle(e).backgroundColor);
            await h.hover();
            await page.waitForFunction(([label, color]) => getComputedStyle(document.querySelector(`[role=slider][aria-label="${label}"]`)).backgroundColor !== color, [await h.getAttribute('aria-label'), normal]);
            assert.equal(await h.evaluate(e => getComputedStyle(e, '::before').opacity), '1');
            await h.focus(); await page.keyboard.press('ArrowRight');
            assert.equal(await h.evaluate(e => getComputedStyle(e).outlineWidth), '2px');
            await page.getByLabel('Размер').selectOption('M');
            await page.waitForFunction(() => document.querySelector('.admiral-slider').classList.contains('admiral-slider--m'));
            assert.equal(Math.round((await h.boundingBox()).width), 16);
            await check('Range'); await page.mouse.move(0, 0);
            await page.getByLabel('Range', { exact: true }).focus();
            assert.equal(await handles.nth(1).evaluate(e => getComputedStyle(e, '::before').opacity), '0');
            await page.screenshot({ path: `${output}/${theme}-desktop.png` });
        });
        await test('Pointer drag snaps to fractional step and updates tooltip', async () => {
            const h = handles.first();
            const box = await h.boundingBox(), track = await slider.locator('.rz-slider').boundingBox();
            await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
            await page.mouse.down();
            await page.mouse.move(track.x + track.width * 0.7, box.y + box.height / 2, { steps: 8 });
            await page.mouse.up();
            await page.waitForFunction(() => Number(document.querySelector('.admiral-slider [role=slider]').getAttribute('aria-valuenow')) !== 5.25);
            const now = Number(await h.getAttribute('aria-valuenow'));
            assert.equal(now * 4, Math.round(now * 4));
            assert.ok(now > 6 && now < 8);
        });
        await test('Disabled and skeleton remove all tab stops', async () => {
            await check('Disabled');
            await page.waitForFunction(() => document.querySelector('.admiral-slider [role=slider]').tabIndex === -1);
            assert.equal(await handles.first().getAttribute('aria-disabled'), 'true');
            assert.ok(await slider.getByRole('button').evaluateAll(es => es.every(e => e.disabled)));
            await check('Disabled', false); await check('Skeleton');
            await page.waitForFunction(() => document.querySelector('.admiral-slider').getAttribute('aria-busy') === 'true');
            assert.equal(await slider.locator('[role=slider]').first().evaluate(e => getComputedStyle(e).visibility), 'hidden');
            await page.screenshot({ path: `${output}/${theme}-skeleton.png` });
        });
        await test('Legacy integer wrapper remains compatible', async () => {
            const legacy = page.getByRole('slider', { name: 'Целочисленный ползунок' });
            await legacy.focus(); await page.keyboard.press('ArrowRight'); await valueIs(legacy, 55);
        });
        await test('Narrow viewport, direct Radzen example and API documentation', async () => {
            for (const width of [1920, 1280, 1000, 800, 600]) {
                await page.setViewportSize({ width, height: 1000 });
                const layout = await slider.evaluate(e => {
                    const preview = e.closest('.admiral-component-playground__preview');
                    const controls = preview.parentElement.querySelector('.admiral-component-controls');
                    return { slider: e.getBoundingClientRect().toJSON(), preview: preview.getBoundingClientRect().toJSON(), controls: controls.getBoundingClientRect().toJSON() };
                });
                assert.ok(layout.slider.x >= layout.preview.x && layout.slider.right <= layout.preview.right, 'Slider stays within its preview');
                assert.ok(layout.controls.x >= layout.preview.right || layout.controls.y >= layout.preview.bottom, 'Controls must not overlap preview');
                if (width === 1280) assert.ok(layout.slider.y - layout.preview.y > 40, 'Sample is centered instead of pinned to preview top');
            }
            await page.setViewportSize({ width: 600, height: 900 });
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            await page.screenshot({ path: `${output}/${theme}-narrow.png` });
            await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
            const h = page.getByRole('slider', { name: 'Прямой Radzen' });
            await h.focus(); await page.keyboard.press('ArrowRight'); await valueIs(h, 3.75);
            await page.locator('.component-doc-tabs [data-doc-tab="api"]').click();
            await page.getByRole('table', { name: 'AdmiralNumericSlider', exact: true }).waitFor();
        });
        await test('AnyStep continuous values and preserved keyboard increment', async () => {
            await check('AnyStep');
            await page.waitForFunction(() => document.querySelector('.admiral-slider').dataset.sliderStep === 'any');
            const track = await slider.locator('.rz-slider').boundingBox();
            await page.mouse.click(track.x + track.width * 0.6337, track.y + track.height / 2);
            await page.waitForFunction(() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1');
            const value = Number(await handles.first().getAttribute('aria-valuenow'));
            assert.ok(Math.abs(value - 6.337) < 0.04);
            assert.ok(Math.abs(value * 4 - Math.round(value * 4)) > 0.05);
            await handles.first().focus(); await page.keyboard.press('ArrowRight');
            await page.waitForFunction(value => Number(document.querySelector('.admiral-slider [role=slider]').getAttribute('aria-valuenow')) > value + 0.24, value);
            assert.ok(Math.abs(Number(await handles.first().getAttribute('aria-valuenow')) - value - 0.25) < 0.000001);
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-drag-ends'), '1');
        });
        await test('Coincident endpoints can separate in both directions', async () => {
            await page.getByRole('button', { name: 'Совместить маркеры', exact: true }).click();
            await page.getByRole('slider', { name: 'Нижняя граница' }).waitFor();
            await valueIs(handles.first(), 5); await valueIs(handles.nth(1), 5);
            await handles.first().focus(); await page.keyboard.press('ArrowLeft'); await valueIs(handles.first(), 4.75);
            await handles.nth(1).focus(); await page.keyboard.press('ArrowRight'); await valueIs(handles.nth(1), 5.25);
            await page.getByRole('button', { name: 'Совместить маркеры', exact: true }).click();
            await valueIs(handles.first(), 5); await valueIs(handles.nth(1), 5);
            const track = await slider.locator('.rz-slider').boundingBox();
            await page.mouse.click(track.x + track.width * 0.3, track.y + track.height / 2);
            await valueIs(handles.first(), 3); await valueIs(handles.nth(1), 5);
        });
        await test('Pointer crossing retains capture and follows exchanged role', async () => {
            await check('Range'); await page.getByRole('slider', { name: 'Нижняя граница' }).waitFor();
            const box = await handles.first().boundingBox(), track = await slider.locator('.rz-slider').boundingBox();
            await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2); await page.mouse.down();
            await page.mouse.move(track.x + track.width * 0.9, box.y + box.height / 2, { steps: 12 });
            await valueIs(handles.first(), 7.75); await valueIs(handles.nth(1), 9);
            assert.equal(await handles.nth(1).evaluate(e => e === document.activeElement), true);
            await page.mouse.move(track.x + track.width * 0.1, box.y + box.height / 2, { steps: 12 });
            await valueIs(handles.first(), 1); await valueIs(handles.nth(1), 7.75);
            assert.equal(await handles.first().evaluate(e => e === document.activeElement), true);
            await page.mouse.up();
            await page.waitForFunction(() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1');
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-completion-matched'), 'true');
        });
        await test('Pointer completion once, only after final bound value; marks do not complete', async () => {
            const box = await handles.first().boundingBox(), track = await slider.locator('.rz-slider').boundingBox();
            await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2); await page.mouse.down();
            await page.mouse.move(track.x + track.width * 0.8, box.y + box.height / 2, { steps: 10 });
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-drag-ends'), '0');
            await page.mouse.up(); await valueIs(handles.first(), 8);
            await page.waitForFunction(() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1');
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-completion-matched'), 'true');
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-last-cancelled'), 'false');
            await slider.getByRole('button', { name: '0', exact: true }).click(); await valueIs(handles.first(), 0);
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-drag-ends'), '1');
        });
        await test('Pointer cancellation releases gesture and never double-completes', async () => {
            const box = await handles.first().boundingBox(), track = await slider.locator('.rz-slider').boundingBox();
            const start = { pointerId: 77, pointerType: 'touch', clientX: box.x + box.width / 2, clientY: box.y + box.height / 2, bubbles: true, buttons: 1 };
            await handles.first().dispatchEvent('pointerdown', start);
            await slider.locator('.rz-slider').dispatchEvent('pointermove', { ...start, clientX: track.x + track.width * 0.7 });
            await slider.locator('.rz-slider').dispatchEvent('pointercancel', start);
            await slider.locator('.rz-slider').dispatchEvent('lostpointercapture', start);
            await slider.locator('.rz-slider').dispatchEvent('pointerup', start);
            await page.waitForFunction(() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1');
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-last-cancelled'), 'true');
            assert.equal(await slider.locator('.admiral-slider__handle--dragging').count(), 0);
            await handles.first().focus(); await page.keyboard.press('Home'); await valueIs(handles.first(), 0);
        });
        await test('Browser-emulated touch drag, not a physical-device claim', async () => {
            const session = await context.newCDPSession(page);
            try {
                const box = await handles.first().boundingBox(), track = await slider.locator('.rz-slider').boundingBox();
                const point = { id: 1, x: box.x + box.width / 2, y: box.y + box.height / 2 };
                await session.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [point] });
                await session.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ ...point, x: track.x + track.width * 0.7 }] });
                await session.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
                await page.waitForFunction(() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1');
                assert.equal(await page.locator('[data-slider-result]').getAttribute('data-last-cancelled'), 'false');
                assert.ok(Number(await handles.first().getAttribute('aria-valuenow')) > 6);
            } finally { await session.detach(); }
        });
        await test('Rapid keyboard actions are not coalesced; custom attributes and Page keys', async () => {
            const h = handles.first();
            assert.equal(await h.getAttribute('data-testid'), 'single-handle');
            assert.ok((await h.getAttribute('class')).includes('sample-slider-handle'));
            assert.equal(await h.getAttribute('title'), 'Настраиваемый HTML-атрибут');
            await h.focus();
            for (let i = 0; i < 8; i++) await page.keyboard.press('ArrowRight');
            await valueIs(h, 7.25);
            await page.waitForFunction(() => document.querySelector('[data-slider-result]').dataset.changes === '8');
            await page.keyboard.press('PageDown'); await valueIs(h, 4.75);
            await page.keyboard.press('PageUp'); await valueIs(h, 7.25);
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-drag-ends'), '0');
            await check('Range');
            await page.getByTestId('lower-handle').waitFor(); await page.getByTestId('upper-handle').waitFor();
        });
        await test('External reset and disabled-mid-drag discard stale packets', async () => {
            const box = await handles.first().boundingBox(), track = await slider.locator('.rz-slider').boundingBox();
            const start = { pointerId: 99, pointerType: 'touch', clientX: box.x + box.width / 2, clientY: box.y + box.height / 2, bubbles: true, buttons: 1 };
            await handles.first().dispatchEvent('pointerdown', start);
            await slider.locator('.rz-slider').dispatchEvent('pointermove', { ...start, clientX: track.x + track.width * 0.8 });
            await page.getByRole('button', { name: 'Сбросить значение', exact: true }).click();
            await valueIs(handles.first(), 5.25);
            await slider.locator('.rz-slider').dispatchEvent('pointerup', start);
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-drag-ends'), '0');
            await handles.first().dispatchEvent('pointerdown', start);
            await check('Disabled');
            await page.waitForFunction(() => document.querySelector('.admiral-slider [role=slider]').tabIndex === -1);
            await slider.locator('.rz-slider').dispatchEvent('pointerup', start);
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-drag-ends'), '0');
        });
        await test('Changing mode during a gesture ignores incompatible response shapes', async () => {
            const box = await handles.first().boundingBox(), track = await slider.locator('.rz-slider').boundingBox();
            const start = { pointerId: 109, pointerType: 'touch', clientX: box.x + box.width / 2, clientY: box.y + box.height / 2, bubbles: true, buttons: 1 };
            await handles.first().dispatchEvent('pointerdown', start);
            await slider.locator('.rz-slider').dispatchEvent('pointermove', { ...start, clientX: track.x + track.width * 0.8 });
            await check('Range'); await page.getByRole('slider', { name: 'Нижняя граница' }).waitFor();
            await slider.locator('.rz-slider').dispatchEvent('pointerup', start);
            await page.waitForTimeout(150);
            assert.equal(await page.locator('[data-slider-result]').getAttribute('data-drag-ends'), '0');
            assert.ok(await handles.evaluateAll(elements => elements.every(e => Number.isFinite(Number(e.getAttribute('aria-valuenow'))) && !e.style.cssText.includes('NaN'))));
            await valueIs(handles.first(), 2.25); await valueIs(handles.nth(1), 7.75);
        });
        await test('Handled arrows stay inside slider while Tab reaches the parent', async () => {
            await page.evaluate(() => {
                window.sliderBubbledKeys = [];
                document.addEventListener('keydown', event => window.sliderBubbledKeys.push(event.key));
            });
            await handles.first().focus(); await page.keyboard.press('ArrowRight'); await valueIs(handles.first(), 5.5);
            await page.keyboard.press('Tab');
            const keys = await page.evaluate(() => window.sliderBubbledKeys);
            assert.ok(!keys.includes('ArrowRight')); assert.ok(keys.includes('Tab'));
        });
        await test('Off-center thumb click preserves value and completes once', async () => {
            const box = await handles.first().boundingBox();
            await page.mouse.click(box.x + box.width / 2 + 5, box.y + box.height / 2);
            await page.waitForFunction(() => document.querySelector('[data-slider-result]').dataset.dragEnds === '1');
            await valueIs(handles.first(), 5.25);
        });
        await test('Negative minimum anchors fractional steps and off-grid maximum remains reachable', async () => {
            const h = page.getByRole('slider', { name: 'Смещённый диапазон', exact: true });
            await h.focus(); await page.keyboard.press('Home'); await valueIs(h, -2.1);
            await page.keyboard.press('ArrowRight'); await valueIs(h, -1.9);
            await page.keyboard.press('End'); await valueIs(h, 3.05);
            await page.keyboard.press('ArrowLeft'); await valueIs(h, 2.9);
        });
        await context.close();
    }
} finally { await browser.close(); }
await writeFile(`${output}/slider-interactions.json`, JSON.stringify(results, null, 2));
process.exitCode = results.some(r => !r.passed) ? 1 : 0;
