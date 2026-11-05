import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const base = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/native-date-range';
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true,
    ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const results = [];
try {
    for (const theme of ['admiral', 'admiral-dark']) {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        await context.addCookies([
            { name: 'RadzenAdmiralTheme', value: theme, url: base },
            { name: '.AspNetCore.Culture', value: 'c=en|uic=en', url: base }
        ]);
        const page = await context.newPage();
        page.setDefaultTimeout(6000);
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        try {
            await page.goto(base + '/regression/inputs');
            const root = page.locator('#input-native-range');
            const input = root.locator('input');
            const popup = page.locator('#popupinput-native-range');
            await input.waitFor();
            await page.waitForTimeout(300);
            await root.locator('.rz-datepicker-trigger').click();
            await popup.waitFor({ state: 'visible' });
            await page.waitForTimeout(300);
            assert.equal(await popup.locator('.rz-calendar-view').count(), 2);
            const days = popup.locator('.rz-calendar-view').first().locator('td[id]');
            const start = await days.nth(4).getAttribute('id');
            const end = await days.nth(7).getAttribute('id');
            const typedRange = await days.evaluateAll(cells => cells.slice(10, 14).map(cell => cell.id));
            const formatted = id => id.slice(-10).split('-').reverse().join('.');
            await days.nth(4).click();
            await days.nth(7).click();
            await popup.waitFor({ state: 'hidden' });
            assert.equal(await input.inputValue(), `${formatted(start)} - ${formatted(end)}`);
            await root.locator('.rz-dropdown-clear-icon').click();
            await page.waitForFunction(() => document.querySelector('#input-native-range input').value === '');
            // Keep the typed range in the displayed month, distinct from the clicked range.
            await input.fill(`${formatted(typedRange[0])} - ${formatted(typedRange[3])}`);
            await input.press('Tab');
            await root.locator('.rz-datepicker-trigger').click();
            await popup.waitFor({ state: 'visible' });
            await page.waitForFunction(({ ids }) => {
                const selected = document.querySelectorAll('#popupinput-native-range td[aria-selected="true"]');
                return selected.length === ids.length && [...selected].every(cell => ids.includes(cell.id));
            }, { ids: typedRange });
            await page.waitForTimeout(300);
            await popup.screenshot({ path: `${output}/${theme}.png` });
            await input.focus();
            await input.press('Escape');
            await popup.waitFor({ state: 'hidden' });
            await page.goto(base + '/components/date-range-picker');
            await page.locator('.admiral-consumer-shell[data-interactive="true"]').waitFor();
            const demo = page.getByTestId('date-range-admiral-demo');
            const range = demo.getByRole('textbox', { name: 'Date range', exact: true });
            const status = demo.getByRole('status');
            const initial = await range.inputValue();
            await range.press('Enter');
            await range.fill('07.10.2026 — 01.10.2026');
            await range.press('Enter');
            await demo.locator('.admiral-date-range-picker__input[aria-invalid="true"]').waitFor();
            assert.match(await status.innerText(), /01.10.2026.*07.10.2026.*Changes: 0/);
            await range.press('Escape');
            await demo.locator('.admiral-date-range-picker__input[aria-invalid="false"]').waitFor();
            assert.equal(await range.inputValue(), initial);

            await range.fill('01.10.2026 — 08.10.2026');
            await range.press('Enter');
            await status.getByText(/08.10.2026.*Changes: 1/).waitFor();
            await range.fill('');
            await range.press('Tab');
            await status.getByText(/Start: —.*End: —.*Changes: 3/).waitFor();

            await demo.locator('.rz-datepicker-trigger').click();
            const calendar = page.locator('.rz-datepicker-popup-container:visible');
            const cells = calendar.locator('td[id]:not([aria-disabled="true"])');
            await cells.nth(4).press('Enter');
            await status.getByText(/Changes: 4/).waitFor();
            await cells.nth(7).press('Enter');
            await status.getByText(/Changes: 5/).waitFor();
            const firstBoundary = calendar.locator('td.admiral-date-range-picker__day--start');
            const lastBoundary = calendar.locator('td.admiral-date-range-picker__day--end');
            const firstDate = (await firstBoundary.getAttribute('id')).slice(-10);
            const lastDate = (await lastBoundary.getAttribute('id')).slice(-10);
            assert.ok(firstDate < lastDate);
            const inside = await calendar.locator('td.admiral-date-range-picker__day--inside')
                .evaluateAll(days => days.map(day => day.id.slice(-10)));
            const dayCount = (Date.parse(lastDate) - Date.parse(firstDate)) / 86400000;
            assert.equal(inside.length, dayCount - 1);
            assert.ok(inside.every(date => date > firstDate && date < lastDate));
            assert.deepEqual(errors, []);
            results.push({ theme, passed: true });
        } catch (error) {
            results.push({ theme, passed: false, error: error.stack });
        }
        await context.close();
    }
} finally {
    await browser.close();
    await writeFile(`${output}/results.json`, JSON.stringify(results, null, 2));
}
console.log(JSON.stringify(results, null, 2));
if (results.some(result => !result.passed)) process.exitCode = 1;
