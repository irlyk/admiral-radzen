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
const closeTo = (actual, expected, tolerance = 1) => Math.abs(actual - expected) <= tolerance;

try {
  for (const theme of ['admiral', 'admiral-dark']) {
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
      { name: '.AspNetCore.Culture', value: 'c=en|uic=en', url: baseURL }]);
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
        assert.deepEqual(errors, []);
        assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
        results.push({ theme, name, passed: true });
      } catch (error) {
        results.push({ theme, name, passed: false, error: error.stack });
      } finally {
        page.off('pageerror', onError);
      }
      console.log(theme, name, results.at(-1).passed ? 'PASS' : 'FAIL');
    };

    await test('GroupActions text action focus and alignment across sizes', async () => {
      await go('/components/action-bar');
      const preview = page.locator('.action-bar-demo-preview');
      const pane = preview.locator('.admiral-group-actions-pane');
      const action = pane.locator('.admiral-group-actions-pane__actions > button').first();
      const size = page.locator('.admiral-component-controls select').first();
      for (const option of ['Small', 'Medium', 'Large', 'ExtraLarge']) {
        await size.selectOption(option);
        await action.waitFor();
        await page.keyboard.press('Tab');
        await action.focus();
        const styles = await action.evaluate(element => {
          const style = getComputedStyle(element);
          const clip = getComputedStyle(element.parentElement);
          return { outline: style.outlineStyle, outlineWidth: parseFloat(style.outlineWidth),
            outlineOffset: parseFloat(style.outlineOffset), clipMargin: parseFloat(clip.overflowClipMargin) || 0 };
        });
        assert.notEqual(styles.outline, 'none', `Focus outline is present at ${option}`);
        assert.ok(styles.outlineWidth > 0, `Focus outline has width at ${option}`);
        assert.ok(styles.clipMargin >= styles.outlineWidth + Math.max(styles.outlineOffset, 0),
          `Focus outline fits inside clip margin at ${option}`);
        const parent = await pane.boundingBox();
        const button = await action.boundingBox();
        assert.ok(parent && button);
        assert.ok(closeTo(button.y + button.height / 2, parent.y + parent.height / 2, 2),
          `Action is vertically aligned at ${option}`);
        assert.ok(button.height <= parent.height + 1, `Action fits pane height at ${option}`);
      }
    });

    await test('Pagination dropdown geometry in default, hover, focus and open states', async () => {
      await go('/tables');
      const table = page.locator('section[aria-labelledby="table-l-heading"]');
      const pagination = table.locator('.admiral-pagination-one:not(.admiral-pagination-one--simple)');
      await pagination.waitFor();
      const selects = pagination.locator('.admiral-pagination-one__select.rz-dropdown');
      assert.equal(await selects.count(), 2);
      for (let i = 0; i < 2; i++) {
        const select = selects.nth(i);
        const base = await select.boundingBox();
        const label = await select.locator('.rz-dropdown-label').boundingBox();
        assert.ok(base && label);
        assert.ok(label.x >= base.x - 1 && label.x + label.width <= base.x + base.width + 1,
          `Dropdown label is contained by outer control ${i}`);
        const geometry = async () => {
          const box = await select.boundingBox();
          const border = await select.evaluate(e => getComputedStyle(e).borderTopWidth);
          return { box, border: parseFloat(border) };
        };
        const start = await geometry();
        await select.hover();
        const hover = await geometry();
        await page.keyboard.press('Tab');
        await select.focus();
        await page.waitForTimeout(250);
        const focus = await geometry();
        const focusStyle = await select.evaluate(e => {
          const style = getComputedStyle(e);
          const probe = document.createElement('span');
          probe.style.color = style.getPropertyValue('--rz-primary').trim();
          e.appendChild(probe);
          const primary = getComputedStyle(probe).color;
          probe.remove();
          return { visible: e.matches(':focus-visible'), color: style.borderTopColor,
            width: style.borderTopWidth, primary };
        });
        assert.ok(focusStyle.visible, `Dropdown ${i} has keyboard focus-visible`);
        assert.equal(focusStyle.width, '2px', `Dropdown ${i} focus border is 2px`);
        assert.equal(focusStyle.color, focusStyle.primary,
          `Dropdown ${i} focus border uses the primary color`);
        await page.locator('h1').click();
        await select.click();
        await page.locator('.rz-dropdown-panel:visible').first().waitFor();
        await page.waitForTimeout(250);
        const open = await geometry();
        const mouseStyle = await select.evaluate(e => ({
          visible: e.matches(':focus-visible'), color: getComputedStyle(e).borderTopColor,
          width: getComputedStyle(e).borderTopWidth,
        }));
        assert.equal(mouseStyle.visible, false, `Dropdown ${i} mouse focus has no keyboard ring`);
        assert.equal(mouseStyle.color, 'rgba(0, 0, 0, 0)',
          `Dropdown ${i} mouse focus border remains transparent`);
        assert.equal(mouseStyle.width, '2px', `Dropdown ${i} mouse focus preserves border geometry`);
        for (const [state, value] of Object.entries({ hover, focus, open })) {
          assert.ok(closeTo(value.box.width, start.box.width) && closeTo(value.box.height, start.box.height),
            `Dropdown ${i} geometry remains stable in ${state}`);
          assert.ok(closeTo(value.border, start.border), `Dropdown ${i} border remains stable in ${state}`);
        }
        await page.keyboard.press('Escape');
      }
      await go('/regression/inputs');
      const direct = page.locator('#input-currency.rz-dropdown').first();
      await direct.waitFor();
      assert.equal(await direct.evaluate(e => getComputedStyle(e).borderTopWidth), '1px',
        'Direct Radzen dropdown retains its 1px input border');
    });

    await test('Select-family popups fit a short viewport and retain keyboard selection', async () => {
      const viewport = { width: 500, height: 350 };
      await page.setViewportSize(viewport);
      const checkPopup = async (trigger, popup, name) => {
        await popup.waitFor();
        await page.waitForTimeout(300);
        const anchor = await trigger.boundingBox();
        const panel = await popup.boundingBox();
        assert.ok(anchor && panel && panel.width > 0 && panel.height > 0, `${name}: visible trigger and popup`);
        assert.ok(panel.x >= -1 && panel.x + panel.width <= viewport.width + 1 &&
          panel.y >= -1 && panel.y + panel.height <= viewport.height + 1,
        `${name}: popup fits short viewport ${JSON.stringify(panel)}`);
        assert.ok(panel.y + panel.height <= anchor.y + 2 || panel.y >= anchor.y + anchor.height - 2,
          `${name}: popup does not overlap trigger`);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
          `${name}: no horizontal overflow`);
        const list = popup.locator('.rz-dropdown-items-wrapper');
        if (await list.count()) {
          const metrics = await list.evaluate(e => ({ scrollHeight: e.scrollHeight, clientHeight: e.clientHeight }));
          if (metrics.scrollHeight > metrics.clientHeight + 2) {
            await list.evaluate(e => { e.scrollTop = e.scrollHeight; });
            assert.ok(await list.evaluate(e => e.scrollTop > 0), `${name}: options remain scrollable`);
          }
        }
      };
      try {
        await go('/components/select');
        const select = page.locator('.admiral-component-playground .rz-dropdown').first();
        await select.focus();
        await page.keyboard.press('Enter');
        const popup = page.locator('.rz-dropdown-panel:visible').last();
        await checkPopup(select, popup, 'AdmiralSelectField');
        await page.keyboard.press('ArrowDown');
        await page.keyboard.press('Enter');
        await popup.waitFor({ state: 'hidden' });

        await go('/components/drop-down');
        const admiralDropDown = page.locator('[data-testid="drop-down-admiral-demo"] .rz-dropdown').first();
        await admiralDropDown.focus();
        await page.keyboard.press('Enter');
        const admiralPopup = page.locator('.rz-dropdown-panel:visible').last();
        await checkPopup(admiralDropDown, admiralPopup, 'AdmiralDropDown');
        await page.keyboard.press('Escape');
        await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
        const direct = page.locator('[data-testid="drop-down-radzen-demo"] .rz-dropdown').first();
        await direct.focus();
        await page.keyboard.press('Enter');
        const directPopup = page.locator('.rz-dropdown-panel:visible').last();
        await checkPopup(direct, directPopup, 'direct RadzenDropDown');
        await page.keyboard.press('Escape');

        await go('/components/phone-number-input');
        const phone = page.locator('.admiral-phone-input .rz-dropdown').first();
        await phone.focus();
        await page.keyboard.press('Enter');
        const phonePopup = page.locator('.rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)').first();
        await checkPopup(phone, phonePopup, 'PhoneNumberInput country Select');
        await page.keyboard.press('Escape');
      } finally {
        await page.setViewportSize({ width: 1440, height: 900 });
      }
    });

    await test('Phone country popup stays within viewport and responds to keyboard', async () => {
      for (const [width, height] of [[240, 800], [320, 800], [390, 800], [320, 320]]) {
        await page.setViewportSize({ width, height });
        await go('/components/phone-number-input');
        const select = page.locator('.admiral-phone-input .rz-dropdown').first();
        await select.click();
        const popup = page.locator('.rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)').first();
        await popup.waitFor();
        await page.waitForTimeout(300); // Wait for the popup's opening animation before measuring it.
        const box = await popup.boundingBox();
        assert.ok(box && box.x >= -1 && box.x + box.width <= width + 1 &&
          box.y >= -1 && box.y + box.height <= height + 1,
        `Phone country popup fits ${width}×${height}px viewport`);
        const list = await popup.locator('.rz-dropdown-items-wrapper').boundingBox();
        const dialCode = await popup.locator('.admiral-phone-input__dial-code').first().boundingBox();
        assert.ok(list && dialCode && list.x >= box.x - 1 && list.x + list.width <= box.x + box.width + 1 &&
          dialCode.x >= list.x - 1 && dialCode.x + dialCode.width <= list.x + list.width + 1,
        `Phone country list and dial code fit ${width}×${height}px popup`);
        const search = popup.locator('input').first();
        await search.fill('United');
        await page.waitForFunction(() => document.querySelectorAll('.rz-dropdown-panel.rz-open .rz-dropdown-item').length === 2);
        await page.waitForTimeout(350); // Radzen resets its keyboard index after filtered options render.
        await search.press('ArrowDown');
        await popup.locator('.rz-dropdown-item.rz-state-highlight').first().waitFor();
        assert.equal(await search.inputValue(), 'United');
        await search.press('Enter');
        await page.waitForFunction(() => /United/.test(document.querySelector('.admiral-phone-input .rz-dropdown')?.getAttribute('aria-label') ?? ''));
        await popup.waitFor({ state: 'hidden' });
        await select.click();
        await page.locator('.rz-dropdown-panel:visible').first().waitFor();
        await page.keyboard.press('Escape');
        await page.locator('.rz-dropdown-panel:visible').first().waitFor({ state: 'hidden' });
      }
    });

    await test('Checkbox demos begin unchecked in Admiral and direct Radzen tabs', async () => {
      await go('/components/checkbox');
      for (const [tab, demo, result] of [
        ['admiral', 'checkbox-admiral-demo', 'checkbox-admiral-result'],
        ['theme', 'checkbox-radzen-demo', 'checkbox-radzen-result']
      ]) {
        await page.locator(`.component-doc-tabs [data-doc-tab="${tab}"]`).click();
        const checkbox = page.getByTestId(demo).getByRole('checkbox').first();
        await checkbox.waitFor();
        assert.equal(await checkbox.getAttribute('aria-checked'), 'false');
        assert.match(await page.getByTestId(result).innerText(), /Changes: 0|Изменений: 0/);
        const box = page.getByTestId(demo).locator('.rz-chkbox-box').first();
        assert.deepEqual(await box.evaluate(element => {
          const style = getComputedStyle(element);
          return [style.transitionProperty, style.transitionDuration, style.transitionTimingFunction];
        }), ['background-color', '0.1s', 'ease-in']);
        await page.emulateMedia({ reducedMotion: 'reduce' });
        assert.equal(await box.evaluate(element => getComputedStyle(element).transitionProperty), 'none');
        await page.emulateMedia({ reducedMotion: 'no-preference' });
      }
    });

    await context.close();
  }
} finally {
  if (output) {
    await mkdir(output, { recursive: true });
    await writeFile(path.join(output, 'visual-regression.json'), JSON.stringify(results, null, 2));
  }
  await browser.close();
}

if (results.some(result => !result.passed)) process.exitCode = 1;
