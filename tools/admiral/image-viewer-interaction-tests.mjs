import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/image-viewer-tests';
const results = [];
await mkdir(output, { recursive: true });
try {
  for (const theme of ['admiral', 'admiral-dark']) {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: baseURL }]);
    const page = await context.newPage();
    page.setDefaultTimeout(7000);
    const overlay = page.locator('.admiral-image-viewer__overlay');
    const preview = overlay.locator('.admiral-image-viewer__preview');
    const surface = overlay.locator('.admiral-image-viewer__surface');
    const open = async () => {
      await page.goto(baseURL + '/components/image-viewer');
      await page.waitForTimeout(500);
      await page.getByRole('button', { name: 'Открыть: Горный пейзаж', exact: true }).click();
      await preview.waitFor();
      const bounds = await overlay.boundingBox();
      const layoutBounds = await page.locator('.admiral-layout').boundingBox();
      const viewport = page.viewportSize();
      assert.ok(Math.abs(bounds.x) < 1 && Math.abs(bounds.y) < 1, 'Dialog must start at viewport origin');
      assert.ok(Math.abs(bounds.width - layoutBounds.width) < 1 && Math.abs(bounds.height - viewport.height) < 1, 'Dialog must cover header/sidebar too');
      await page.waitForFunction(() => {
        const image = document.querySelector('.admiral-image-viewer__preview');
        return image?.complete && image.naturalWidth > 0 && !document.querySelector('.admiral-image-viewer__message');
      });
    };
    const scale = () => preview.evaluate(e => Number(e.style.transform.match(/scale\(([^)]+)\)/)?.[1]));
    const waitScale = expected => page.waitForFunction(expected => {
      const value = Number(document.querySelector('.admiral-image-viewer__preview')?.style.transform.match(/scale\(([^)]+)\)/)?.[1]);
      return Math.abs(value - expected) < 0.001;
    }, expected);
    const zoomMax = async () => {
      for (let i = 0; i < 12 && await scale() < 3; i++) {
        const previous = await scale();
        await overlay.getByRole('button', { name: 'Увеличить', exact: true }).click();
        await waitScale(Math.min(3, previous + 0.25));
      }
      await waitScale(3);
    };
    const test = async (name, action) => {
      const errors = [];
      const onError = error => errors.push(error.message);
      page.on('pageerror', onError);
      try {
        await action();
        assert.deepEqual(errors, []);
        assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
        results.push({ theme, name, passed: true });
      } catch (error) { results.push({ theme, name, passed: false, error: error.stack }); }
      finally { page.off('pageerror', onError); }
      console.log(theme, name, results.at(-1).passed ? 'PASS' : results.at(-1).error);
    };
    await test('wheel and keyboard zoom respect limits without scrolling the document', async () => {
      await open();
      const scrollY = await page.evaluate(() => window.scrollY);
      await preview.hover(); await page.mouse.wheel(0, -200);
      await page.waitForFunction(() => Number(document.querySelector('.admiral-image-viewer__preview').style.transform.match(/scale\(([^)]+)\)/)[1]) > 1);
      assert.equal(await page.evaluate(() => window.scrollY), scrollY);
      await zoomMax();
      assert.equal(await overlay.getByRole('button', { name: 'Увеличить', exact: true }).isDisabled(), true);
      await page.keyboard.press('ArrowDown'); await waitScale(2.75);
      await page.keyboard.press('ArrowUp'); await waitScale(3);
    });
    await test('mouse pan is bounded; navigation and reopening reset transformations', async () => {
      await page.setViewportSize({ width: 600, height: 700 });
      await open(); await zoomMax();
      await page.waitForTimeout(180);
      const bounds = await surface.boundingBox();
      await page.mouse.move(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
      await page.mouse.down(); await page.mouse.move(bounds.x + bounds.width / 2 + 120, bounds.y + bounds.height / 2 + 80, { steps: 8 });
      await page.mouse.up();
      const offset = await surface.evaluate(e => [parseFloat(e.style.getPropertyValue('--admiral-image-pan-x')), parseFloat(e.style.getPropertyValue('--admiral-image-pan-y'))]);
      assert.ok(Math.abs(offset[0]) > 0 || Math.abs(offset[1]) > 0, 'Zoomed image should move');
      await page.keyboard.press('ArrowRight'); await waitScale(1);
      await page.waitForFunction(() => document.querySelector('.admiral-image-viewer__surface').style.getPropertyValue('--admiral-image-pan-x') === '0px');
      await overlay.getByRole('button', { name: 'Повернуть вправо', exact: true }).click();
      await page.keyboard.press('Escape'); await overlay.waitFor({ state: 'detached' });
      await page.getByRole('button', { name: 'Открыть: Городская композиция', exact: true }).click();
      await waitScale(1);
      assert.match(await preview.getAttribute('style'), /rotate\(0deg\)/);
      await page.setViewportSize({ width: 1440, height: 1000 });
    });
    await test('synthetic two-pointer pinch changes scale and releases gesture state', async () => {
      await open();
      await preview.dispatchEvent('pointerdown', { pointerId: 31, pointerType: 'touch', clientX: 400, clientY: 350, button: 0 });
      await preview.dispatchEvent('pointerdown', { pointerId: 32, pointerType: 'touch', clientX: 500, clientY: 350, button: 0 });
      await surface.dispatchEvent('pointermove', { pointerId: 32, pointerType: 'touch', clientX: 550, clientY: 350 });
      await waitScale(1.5);
      await surface.dispatchEvent('pointerup', { pointerId: 31, pointerType: 'touch' });
      await surface.dispatchEvent('pointerup', { pointerId: 32, pointerType: 'touch' });
      assert.equal(await surface.getAttribute('data-dragging'), 'false');
    });
    await test('double-click and dialog Enter toggle bounded native scale', async () => {
      await open();
      const native = await preview.evaluate(e => Math.max(0.5, Math.min(3, e.naturalWidth / e.offsetWidth)));
      await overlay.getByRole('button', { name: 'Увеличить', exact: true }).click(); await waitScale(1.25);
      await preview.dblclick(); await waitScale(native);
      await overlay.focus(); await page.keyboard.press('Enter'); await waitScale(1);
    });
    await test('error controls stay disabled and a later open recovers', async () => {
      await page.goto(baseURL + '/components/image-viewer'); await page.waitForTimeout(500);
      await page.getByRole('button', { name: 'Открыть: Недоступное изображение', exact: true }).click();
      await overlay.getByRole('alert').waitFor();
      assert.equal(await overlay.getByRole('button', { name: 'Увеличить', exact: true }).isDisabled(), true);
      await page.keyboard.press('Escape'); await overlay.waitFor({ state: 'detached' });
      await page.getByRole('button', { name: 'Открыть: Горный пейзаж', exact: true }).click();
      await page.waitForFunction(() => document.querySelector('.admiral-image-viewer__preview')?.complete && !document.querySelector('.admiral-image-viewer__message'));
      assert.equal(await overlay.getByRole('button', { name: 'Увеличить', exact: true }).isEnabled(), true);
    });
    await test('wheel retains its anchor after rotation and flips', async () => {
      await page.setViewportSize({ width: 600, height: 700 });
      for (let turn = 0; turn < 4; turn++) {
        await open();
        for (let step = 0; step < 4; step++) {
          await overlay.getByRole('button', { name: 'Увеличить', exact: true }).click();
          await waitScale(1 + (step + 1) * 0.25);
        }
        for (let rotation = 0; rotation < turn; rotation++) await overlay.getByRole('button', { name: 'Повернуть вправо', exact: true }).click();
        await page.waitForFunction(turn => document.querySelector('.admiral-image-viewer__preview').style.transform.includes(`rotate(${turn * 90}deg)`), turn);
        if (turn % 2) await overlay.getByRole('button', { name: 'Отразить по горизонтали', exact: true }).click();
        await page.waitForTimeout(180);
        const bounds = await surface.boundingBox();
        const point = { x: bounds.x + bounds.width / 2 + 15, y: bounds.y + bounds.height / 2 };
        await page.mouse.move(point.x, point.y); await page.mouse.wheel(0, -100);
        await page.waitForFunction(() => Number(document.querySelector('.admiral-image-viewer__preview').style.transform.match(/scale\(([^)]+)\)/)[1]) > 2);
        await page.waitForFunction(() => Math.abs(parseFloat(document.querySelector('.admiral-image-viewer__surface').style.getPropertyValue('--admiral-image-pan-x'))) > 0.1);
        const pan = await surface.evaluate(e => parseFloat(e.style.getPropertyValue('--admiral-image-pan-x')));
        assert.ok(Math.abs((15 - pan) / await scale() - 15 / 2) < 0.1, `${turn * 90}deg: point under cursor must stay fixed`);
      }
      await page.setViewportSize({ width: 1440, height: 1000 });
    });
    await test('swipe directions and boundaries; short, vertical, cancelled and slow gestures do not navigate', async () => {
      await open();
      const counter = overlay.locator('.admiral-image-viewer__counter');
      const gesture = async (dx, dy, delay = 0, cancelled = false) => {
        const box = await surface.boundingBox();
        const x = box.x + box.width / 2, y = box.y + box.height / 2;
        await preview.dispatchEvent('pointerdown', { pointerId: 71, pointerType: 'touch', clientX: x, clientY: y, button: 0 });
        await surface.dispatchEvent('pointermove', { pointerId: 71, pointerType: 'touch', clientX: x + dx, clientY: y + dy });
        if (delay) await page.waitForTimeout(delay);
        await surface.dispatchEvent(cancelled ? 'pointercancel' : 'pointerup', { pointerId: 71, pointerType: 'touch', clientX: x + dx, clientY: y + dy });
      };
      await gesture(-80, 5); await page.waitForFunction(() => document.querySelector('.admiral-image-viewer__counter').textContent.trim() === '2 / 2');
      await gesture(-80, 0); await page.waitForTimeout(150); assert.equal(await counter.innerText(), '2 / 2');
      await gesture(80, 0); await page.waitForFunction(() => document.querySelector('.admiral-image-viewer__counter').textContent.trim() === '1 / 2');
      for (const args of [[-10, 0], [-30, 80], [-80, 0, 550], [-80, 0, 0, true], [80, 0]]) {
        await gesture(...args); await page.waitForTimeout(150);
        assert.equal(await counter.innerText(), '1 / 2');
      }
      await overlay.focus(); await page.keyboard.press('Space');
      await page.waitForFunction(() => document.querySelector('.admiral-image-viewer__counter').textContent.trim() === '2 / 2');
    });
    await test('browser-emulated touch pinch anchors its centroid and never becomes a swipe', async () => {
      await page.setViewportSize({ width: 600, height: 700 });
      await open();
      for (let step = 0; step < 4; step++) {
        await overlay.getByRole('button', { name: 'Увеличить', exact: true }).click(); await waitScale(1 + (step + 1) * 0.25);
      }
      const box = await surface.boundingBox();
      const cx = box.x + box.width / 2, cy = box.y + box.height / 2;
      const session = await context.newCDPSession(page);
      try {
        await session.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: cx - 30, y: cy, id: 1 }, { x: cx + 30, y: cy, id: 2 }] });
        await session.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: cx - 30, y: cy, id: 1 }, { x: cx + 42, y: cy, id: 2 }] });
        await waitScale(2.4);
        await page.waitForFunction(() => Math.abs(parseFloat(document.querySelector('.admiral-image-viewer__surface').style.getPropertyValue('--admiral-image-pan-x')) - 6) < 0.5);
        await session.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [{ x: cx - 30, y: cy, id: 1 }] });
        await session.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: cx - 110, y: cy, id: 1 }] });
        await session.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
        await page.waitForTimeout(150);
        assert.equal(await overlay.locator('.admiral-image-viewer__counter').innerText(), '1 / 2');
      } finally { await session.detach(); }
      await page.setViewportSize({ width: 1440, height: 1000 });
    });
    await test('toolbar default/hover/press/focus/disabled and close geometry match reference tokens', async () => {
      await open();
      const rotate = overlay.getByRole('button', { name: 'Повернуть вправо', exact: true });
      assert.equal(await rotate.evaluate(e => getComputedStyle(e).color), 'rgb(168, 171, 175)');
      await rotate.hover();
      assert.equal(await rotate.evaluate(e => getComputedStyle(e).color), 'rgb(201, 202, 205)');
      assert.equal(await rotate.evaluate(e => getComputedStyle(e).backgroundColor), 'rgba(0, 0, 0, 0)');
      await page.mouse.down();
      assert.equal(await rotate.evaluate(e => getComputedStyle(e).color), 'rgb(227, 229, 231)');
      await page.mouse.up();
      await overlay.focus(); await page.keyboard.press('Tab');
      const close = overlay.getByRole('button', { name: 'Закрыть', exact: true });
      await close.hover();
      const closeStyle = await close.evaluate(e => {
        const s = getComputedStyle(e), r = e.getBoundingClientRect(), icon = e.querySelector('.admiral-icon').getBoundingClientRect();
        return { width: r.width, height: r.height, radius: s.borderRadius, outline: s.outlineStyle, highlight: getComputedStyle(e, '::before').backgroundColor, icon: icon.width };
      });
      assert.deepEqual(closeStyle, { width: 24, height: 24, radius: '4px', outline: 'solid', highlight: 'rgba(0, 0, 0, 0)', icon: 24 });
      const previous = overlay.getByRole('button', { name: 'Предыдущее изображение', exact: true });
      assert.equal(await previous.evaluate(e => getComputedStyle(e).color), 'rgb(130, 134, 139)');
      await page.keyboard.press('Tab'); await rotate.focus(); await rotate.hover();
      assert.equal(await rotate.evaluate(e => getComputedStyle(e).outlineStyle), 'solid');
      assert.equal(await rotate.evaluate(e => getComputedStyle(e).borderRadius), '4px');
      const rect = await rotate.boundingBox(); const icon = await rotate.locator('.admiral-icon').boundingBox();
      assert.equal(icon.width, 24); assert.ok(Math.abs(icon.x + 12 - rect.x - rect.width / 2) < 0.5);
      await page.screenshot({ path: `${output}/image-viewer-toolbar-${theme}.png` });
    });
    await test('miniature dimensions/icon geometry and 360px toolbar stay inside viewport', async () => {
      await page.goto(baseURL + '/components/image-viewer'); await page.waitForTimeout(500);
      const miniature = page.getByRole('button', { name: 'Открыть: Горный пейзаж', exact: true });
      for (const [dimension, size, iconSize] of [['Xxs', 40, 20], ['Xs', 56, 20], ['S', 72, 24], ['M', 96, 24], ['L', 128, 40], ['Xl', 192, 40]]) {
        await page.getByRole('combobox', { name: 'Dimension', exact: true }).selectOption(dimension);
        await page.waitForFunction(({size}) => document.querySelector('.admiral-image-viewer__miniature').getBoundingClientRect().width === size, {size});
        const icon = await miniature.locator('.admiral-icon').boundingBox();
        assert.equal(icon.width, iconSize); assert.equal(icon.height, iconSize);
      }
      await page.setViewportSize({ width: 360, height: 740 });
      await miniature.click(); await preview.waitFor();
      const toolbar = await overlay.locator('.admiral-image-viewer__toolbar').boundingBox();
      assert.ok(toolbar.x >= 0 && toolbar.x + toolbar.width <= 360);
      const first = overlay.locator('.admiral-image-viewer__toolbar button').first();
      const button = await first.boundingBox(), icon = await first.locator('.admiral-icon').boundingBox();
      assert.ok(Math.abs(button.x + button.width / 2 - icon.x - icon.width / 2) < 0.5);
      await page.screenshot({ path: `${output}/image-viewer-mobile-${theme}.png` });
      await page.setViewportSize({ width: 1440, height: 1000 });
    });
    await test('F fit toggle, native toolbar Enter, focus trap and restore', async () => {
      await open();
      const initial = await scale();
      await page.keyboard.press('f');
      await page.waitForFunction(() => Number(document.querySelector('.admiral-image-viewer__preview').style.transform.match(/scale\(([^)]+)\)/)[1]) !== 1);
      await page.keyboard.press('f'); await waitScale(initial);
      await overlay.getByRole('button', { name: 'Увеличить', exact: true }).focus();
      await page.keyboard.press('Enter'); await waitScale(1.25);
      await overlay.getByRole('button', { name: 'Закрыть', exact: true }).focus();
      await page.keyboard.press('Shift+Tab');
      assert.ok(await page.evaluate(() => !!document.activeElement.closest('.admiral-image-viewer__toolbar')));
      await page.keyboard.press('Tab');
      assert.equal(await overlay.getByRole('button', { name: 'Закрыть', exact: true }).evaluate(e => e === document.activeElement), true);
      await page.screenshot({ path: `${output}/image-viewer-${theme}.png` });
      await page.keyboard.press('Escape'); await overlay.waitFor({ state: 'detached' });
      assert.equal(await page.getByRole('button', { name: 'Открыть: Горный пейзаж', exact: true }).evaluate(e => e === document.activeElement), true);
    });
    await context.close();
  }
} finally {
  await writeFile(`${output}/image-viewer-interactions.json`, JSON.stringify(results, null, 2));
  await browser.close();
}
if (results.some(result => !result.passed)) process.exitCode = 1;
