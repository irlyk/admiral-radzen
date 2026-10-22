import assert from 'node:assert/strict';
import { createRequire } from 'node:module';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const base = process.env.SMOKE_URL || 'http://localhost:5072';
const browser = await chromium.launch({ headless: true,
  ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
let passed = 0;
try {
  for (const theme of ['admiral', 'admiral-dark']) for (const culture of ['ru', 'en']) {
    for (const viewport of [{ width: 1440, height: 900 }, { width: 390, height: 780 }]) {
      const context = await browser.newContext({ viewport });
      await context.addCookies([
        { name: 'RadzenAdmiralTheme', value: theme, url: base },
        { name: '.AspNetCore.Culture', value: `c=${culture}|uic=${culture}`, url: base }
      ]);
      const page = await context.newPage();
      page.setDefaultTimeout(8000);
      const errors = [];
      page.on('pageerror', error => errors.push(error.message));
      try {
        await page.goto(base + '/components/select');
        const playground = page.locator('.admiral-component-playground').first();
        await playground.waitFor();
        await page.waitForTimeout(600);
        const select = playground.locator('.rz-dropdown').first();
        const visibleLabel = playground.locator('.admiral-input-field__label').first();
        const labelText = (await visibleLabel.innerText()).trim();
        const labelId = await visibleLabel.getAttribute('id');
        assert.ok(labelId, 'SelectField visible label has a stable id');
        assert.equal(await select.getAttribute('aria-labelledby'), labelId,
          'SelectField combobox is named by its visible label');
        assert.equal(await playground.getByRole('combobox', { name: labelText }).count(), 1,
          'SelectField has one computed accessible name');
        await visibleLabel.click();
        await page.waitForFunction(id => document.activeElement?.id === id, await select.getAttribute('id'));
        assert.equal(await select.evaluate(element => document.activeElement === element), true,
          'Clicking the visible SelectField label focuses its combobox');
        assert.equal(await playground.evaluate(element => document.activeElement?.getAttribute('aria-hidden') === 'true'), false,
          'Label click must not focus Radzen aria-hidden helper input');
        await page.keyboard.press('Tab');
        assert.equal(await select.evaluate(element => document.activeElement === element), false,
          'Tab leaves the SelectField combobox');
        await page.keyboard.press('Shift+Tab');
        assert.equal(await select.evaluate(element => document.activeElement === element), true,
          'Shift+Tab returns to the same combobox');
        await select.focus();
        await page.keyboard.press('Enter');
        const popup = page.locator('.rz-dropdown-panel:visible').last();
        await popup.waitFor();
        const box = await popup.boundingBox();
        assert.ok(box && box.width > 0 && box.height > 0, 'Select popup rendered');
        assert.ok(box.x >= -1 && box.x + box.width <= viewport.width + 1 &&
          box.y >= -1 && box.y + box.height <= viewport.height + 1,
        `${theme}/${culture}/${viewport.width}: Select popup fits viewport ${JSON.stringify(box)}`);
        const triggerBox = await select.boundingBox();
        assert.ok(triggerBox && (box.y + box.height <= triggerBox.y + 2 ||
          box.y >= triggerBox.y + triggerBox.height - 2),
        `${theme}/${culture}/${viewport.width}: Select popup must not overlap its trigger`);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
          'Select popup does not create horizontal overflow');
        await page.keyboard.press('Escape');
        await popup.waitFor({ state: 'hidden' });
        await page.waitForFunction(id => document.activeElement?.id === id, await select.getAttribute('id'));
        assert.equal(await select.evaluate(element => document.activeElement === element), true,
          'Escape closes SelectField popup and restores focus to its combobox');
        await page.keyboard.press('Tab');
        assert.equal(await select.evaluate(element => document.activeElement === element), false,
          'Tab moves forward from a closed SelectField popup');
        await select.focus();
        await page.keyboard.press('Enter');
        await popup.waitFor();
        await page.keyboard.press('ArrowDown');
        assert.ok(await popup.locator('.rz-state-highlight').count() > 0,
          'ArrowDown highlights a selectable option');
        await page.keyboard.press('ArrowDown');
        await page.keyboard.press('Enter');
        await popup.waitFor({ state: 'hidden' });
        await select.click();
        await popup.waitFor();
        await popup.getByText('Казань', { exact: true }).click();
        await page.waitForFunction(() => document.querySelector('.admiral-component-playground [role="status"]')
          ?.textContent?.includes('KZN'));
        assert.match(await playground.locator('[role="status"]').innerText(), /KZN.*[1-9]/,
          'Selection callback is visible in playground');
        const afterSelection = await playground.locator('[role="status"]').innerText();
        await select.click();
        await popup.waitFor();
        await popup.getByText('Казань', { exact: true }).click();
        await popup.waitFor({ state: 'hidden' });
        assert.equal(await playground.locator('[role="status"]').innerText(), afterSelection,
          'Selecting the current value again does not emit a duplicate callback');
        await select.locator('.rz-dropdown-clear-icon').click();
        await page.waitForFunction(previous => document.querySelector('.admiral-component-playground [role="status"]')
          ?.textContent !== previous, afterSelection);
        const countBeforeClear = Number(/(?:Changes|Изменений):\s*(\d+)/.exec(afterSelection)?.[1]);
        const countAfterClear = Number(/(?:Changes|Изменений):\s*(\d+)/.exec(
          await playground.locator('[role="status"]').innerText())?.[1]);
        assert.equal(countAfterClear, countBeforeClear + 1,
          'Clearing a selected value emits exactly one callback');
        const errorField = page.locator('.admiral-input-field').filter({
          has: page.locator('label.admiral-input-field__label').filter({ hasText: 'Error' })
        }).first();
        assert.equal(await errorField.locator('.rz-dropdown').getAttribute('aria-invalid'), 'true',
          'SelectField error status is exposed to assistive technology');
        await page.goto(base + '/components/drop-down');
        await page.waitForTimeout(600);
        await page.locator('.component-doc-tabs [data-doc-tab="theme"]').click();
        const direct = page.locator('[data-testid="drop-down-radzen-demo"] .rz-dropdown').first();
        await direct.waitFor();
        await direct.focus();
        assert.ok((await direct.boundingBox())?.width > 0, 'Direct RadzenDropDown renders');
        await page.keyboard.press('Enter');
        const directPopup = page.locator('.rz-dropdown-panel:visible').last();
        await directPopup.waitFor();
        const directBox = await direct.boundingBox();
        const directPanelBox = await directPopup.boundingBox();
        assert.ok(directBox && directPanelBox &&
          (directPanelBox.y + directPanelBox.height <= directBox.y + 2 ||
            directPanelBox.y >= directBox.y + directBox.height - 2),
        `${theme}/${culture}/${viewport.width}: direct RadzenDropDown popup must not overlap its trigger`);
        await page.keyboard.press('Escape');
        assert.deepEqual(errors, [], 'No browser page errors');
        passed++;
        console.log(`Select/DropDown ${theme}/${culture}/${viewport.width}: PASS`);
      } finally { await context.close(); }
    }
  }
} finally { await browser.close(); }
console.log(`Select browser regressions: ${passed} PASS`);
