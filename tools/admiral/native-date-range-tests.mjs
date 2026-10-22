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
        await context.addCookies([{ name: 'RadzenAdmiralTheme', value: theme, url: base }]);
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
            const formatted = id => id.slice(-10).split('-').reverse().join('.');
            await days.nth(4).click();
            await days.nth(7).click();
            await popup.waitFor({ state: 'hidden' });
            assert.equal(await input.inputValue(), `${formatted(start)} - ${formatted(end)}`);
            await root.locator('.rz-dropdown-clear-icon').click();
            await page.waitForFunction(() => document.querySelector('#input-native-range input').value === '');
            await input.fill('05.10.2026 - 08.10.2026');
            await input.press('Tab');
            await root.locator('.rz-datepicker-trigger').click();
            await popup.waitFor({ state: 'visible' });
            assert.equal(await popup.locator('[aria-selected="true"]').count(), 4);
            await page.waitForTimeout(300);
            await popup.screenshot({ path: `${output}/${theme}.png` });
            await input.focus();
            await input.press('Escape');
            await popup.waitFor({ state: 'hidden' });
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
