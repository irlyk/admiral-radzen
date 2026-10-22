import assert from 'node:assert/strict';
import { createRequire } from 'node:module';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const base = process.env.SMOKE_URL || 'http://localhost:5072';
const browser = await chromium.launch({ headless: true,
  ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
let count = 0;
try {
  for (const theme of ['admiral', 'admiral-dark']) {
    for (const culture of ['ru', 'en']) {
      for (const viewport of [{ width: 1440, height: 900 }, { width: 390, height: 780 }, { width: 320, height: 360 }]) {
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
          await page.goto(base + '/components/phone-number-input');
          const phone = page.locator('.admiral-phone-input').first();
          await phone.waitFor();
          await page.waitForTimeout(600); // Allow interactive Blazor hydration before keyboard input.
          const select = phone.locator('.rz-dropdown');
          assert.equal(await select.count(), 1);
          const input = phone.locator('input[inputmode="tel"]');
          assert.equal(await input.count(), 1);
          assert.ok(await input.evaluate(element => element.labels?.length > 0), 'Phone input has a programmatically associated label');
          await select.focus();
          await page.keyboard.press('Enter');
          const popup = page.locator('.rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)').first();
          await popup.waitFor();
          await page.waitForTimeout(300);
          const geometry = await popup.evaluate(element => {
            const panel = element.getBoundingClientRect();
            const list = element.querySelector('.rz-dropdown-items-wrapper')?.getBoundingClientRect();
            return { panel: panel.toJSON(), list: list?.toJSON(), viewport: { width: innerWidth, height: innerHeight },
              scrollHeight: element.querySelector('.rz-dropdown-items-wrapper')?.scrollHeight ?? 0 };
          });
          const { panel, list } = geometry;
          assert.ok(panel.width > 0 && panel.height > 0, 'Popup has visible dimensions');
          assert.ok(panel.left >= -1 && panel.right <= viewport.width + 1 && panel.top >= -1 && panel.bottom <= viewport.height + 1,
            `${theme}/${culture}/${viewport.width}: popup fits viewport: ${JSON.stringify(panel)}`);
          assert.ok(list && list.left >= panel.left - 1 && list.right <= panel.right + 1 && list.top >= panel.top - 1 && list.bottom <= panel.bottom + 1,
            'Scrollable country list stays inside popup');
          assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
            'Phone popup does not create horizontal document overflow');
          if (geometry.scrollHeight > list.height + 2) {
            const listElement = popup.locator('.rz-dropdown-items-wrapper');
            await listElement.evaluate(element => { element.scrollTop = element.scrollHeight; });
            assert.ok(await listElement.evaluate(element => element.scrollTop > 0), 'Country list can scroll');
          }
          const search = popup.locator('input').first();
          await search.fill('United');
          await page.waitForFunction(() => document.querySelectorAll('.rz-dropdown-panel.rz-open .rz-dropdown-item').length === 2);
          assert.equal(await popup.locator('.rz-dropdown-item').count(), 2);
          await search.press('ArrowDown');
          await popup.locator('.rz-dropdown-item.rz-state-highlight').first().waitFor();
          await search.press('Enter');
          await popup.waitFor({ state: 'hidden' });
          await page.waitForFunction(() => /United/.test(document.querySelector('.admiral-phone-input .rz-dropdown')?.getAttribute('aria-label') ?? ''));
          await select.click();
          await popup.waitFor();
          await page.keyboard.press('Escape');
          await popup.waitFor({ state: 'hidden' });
          await select.focus();
          await page.keyboard.press('Enter');
          await popup.waitFor();
          await page.keyboard.press('Tab');
          await popup.waitFor({ state: 'hidden' });
          if (viewport.width === 1440) {
            await phone.evaluate(element => {
              element.style.position = 'fixed';
              element.style.right = '4px';
              element.style.top = '4px';
              element.style.width = '240px';
              element.style.zIndex = '1000';
            });
            await select.click();
            await popup.waitFor();
            await page.waitForTimeout(300);
            const edgeBox = await popup.boundingBox();
            assert.ok(edgeBox && edgeBox.x >= -1 && edgeBox.x + edgeBox.width <= viewport.width + 1 &&
              edgeBox.y >= -1 && edgeBox.y + edgeBox.height <= viewport.height + 1,
            'Country popup opened near the top/right viewport edge stays visible');
            await page.keyboard.press('Escape');
            await popup.waitFor({ state: 'hidden' });
            // KAZ and RUS share +7. Editing a KAZ number must not silently select RUS.
            await page.goto(base + '/components/phone-number-input');
            await page.waitForTimeout(600);
            const playground = page.locator('.admiral-component-playground').first();
            const country = playground.locator('.admiral-phone-input .rz-dropdown');
            await country.click();
            const countryPopup = page.locator('.rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)').first();
            await countryPopup.waitFor();
            await countryPopup.getByText('Казахстан', { exact: true }).click();
            await countryPopup.waitFor({ state: 'hidden' });
            await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview [role=status]')
              ?.textContent?.includes('KAZ'));
            await page.waitForTimeout(350); // Let both country and value callbacks settle before typing.
            assert.match(await playground.locator('[role=status]').innerText(), /KAZ/,
              'Country remains KAZ after country-selection callbacks settle');
            const number = playground.locator('input[inputmode="tel"]');
            await number.fill('+7 999 123-45-68');
            await number.press('Tab');
            await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview [role=status]')
              ?.textContent?.includes('+7 999 123-45-68'));
            assert.match(await playground.locator('[role=status]').innerText(), /KAZ/,
              'Typing a shared +7 dial code keeps selected KAZ');

            await playground.getByRole('checkbox', { name: 'Disabled' }).check();
            await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview input[inputmode="tel"]')?.disabled === true);
            assert.equal(await number.isDisabled(), true, 'Disabled phone cannot be edited');
            assert.ok(await country.evaluate(element => element.classList.contains('rz-state-disabled') ||
              element.getAttribute('aria-disabled') === 'true'), 'Disabled country selector cannot open');
            await playground.getByRole('checkbox', { name: 'Disabled' }).uncheck();
            await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview input[inputmode="tel"]')?.disabled === false);
            await playground.locator('.admiral-component-controls input[type=checkbox]').nth(4).check();
            await page.waitForFunction(() => document.querySelector('.admiral-component-playground__preview input[inputmode="tel"]')?.readOnly === true);
            assert.equal(await number.getAttribute('readonly'), '', 'Read-only phone has readonly input');
          }
          assert.deepEqual(errors, [], 'No browser page errors');
          assert.equal(await page.locator('#blazor-error-ui').isVisible(), false);
          count++;
          console.log(`Phone popup ${theme}/${culture}/${viewport.width}×${viewport.height}: PASS`);
        } finally { await context.close(); }
      }
    }
  }
} finally { await browser.close(); }
console.log(`Phone browser regressions: ${count} PASS`);
