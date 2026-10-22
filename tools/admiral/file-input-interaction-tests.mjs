import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
import { mkdir, stat, writeFile } from 'node:fs/promises';

const { chromium } = createRequire(import.meta.url)(process.env.PLAYWRIGHT_MODULE || 'playwright');
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const output = process.env.AUDIT_OUTPUT || 'artifacts/file-input-audit';
const fixture = name => fileURLToPath(new URL(`./fixtures/${name}`, import.meta.url));
const first = fixture('file-input-first.svg'), second = fixture('file-input-second.svg');
const browser = await chromium.launch({ headless: true,
    ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const results = [];
await mkdir(output, { recursive: true });
try {
    for (const theme of ['admiral', 'admiral-dark']) {
        const context = await browser.newContext({ viewport: { width: 1280, height: 1000 } });
        await context.addCookies([
            { name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
            { name: '.AspNetCore.Culture', value: 'c=ru|uic=ru', url: baseURL }
        ]);
        const page = await context.newPage();
        const input = page.locator('.admiral-file-input-control input[type=file]');
        const result = page.locator('.admiral-component-page__result');
        const pick = async (files, custom = false, keyboard = false) => {
            const chooserPromise = page.waitForEvent('filechooser');
            const trigger = page.getByRole('button', { name: custom ? 'Выбрать файлы' : 'Добавьте файлы', exact: true });
            if (keyboard) await trigger.press('Enter'); else await trigger.click();
            await (await chooserPromise).setFiles(files);
        };
        const test = async (name, run) => {
            const errors = [];
            const onError = error => errors.push(error.message);
            page.on('pageerror', onError);
            try {
                await page.goto(`${baseURL}/components/file-input`);
                await input.waitFor();
                // Wait for the interactive circuit, not just prerendered markup.
                await page.getByRole('checkbox', { name: 'Multiple', exact: true }).uncheck();
                await page.waitForFunction(() => !document.querySelector('input[type=file]').multiple);
                await page.getByRole('checkbox', { name: 'Multiple', exact: true }).check();
                await page.waitForFunction(() => document.querySelector('input[type=file]').multiple);
                await run();
                assert.deepEqual(errors, []);
                results.push({ theme, name, passed: true });
            } catch (error) {
                results.push({ theme, name, passed: false, error: error.stack });
                await page.screenshot({ path: `${output}/${theme}-${results.length}-failed.png` });
            } finally { page.off('pageerror', onError); }
        };
        await test('External removal preserves native FileList and remaining streams', async () => {
            await pick([first, second]);
            await page.getByText('file-input-second.svg', { exact: true }).waitFor();
            await page.getByRole('button', { name: 'Удалить первый извне', exact: true }).click();
            await page.getByText('file-input-first.svg', { exact: true }).waitFor({ state: 'hidden' });
            await page.waitForFunction(() => document.querySelector('input[type=file]').files.length === 1);
            assert.deepEqual(await input.evaluate(element => Array.from(element.files, file => file.name)), ['file-input-second.svg']);
            await page.getByRole('button', { name: 'Прочитать потоки', exact: true }).click();
            await page.getByText(`Выбрано: 1 · Последнее действие: Прочитано ${(await stat(second)).size} байт`, { exact: true }).waitFor();
        });
        await test('Rejected selection clears stale list and reports accessible error', async () => {
            await pick([first]);
            await page.getByText('file-input-first.svg', { exact: true }).waitFor();
            await pick([fixture('file-input-rejected.txt')]);
            await page.getByText('Недопустимый формат файла', { exact: true }).waitFor();
            await page.getByText('file-input-first.svg', { exact: true }).waitFor({ state: 'hidden' });
            assert.equal(await input.getAttribute('aria-invalid'), 'true');
            assert.match(await result.innerText(), /Выбрано: 0/);
        });
        await test('Size/count limits, reset and same-file reselection', async () => {
            await page.getByRole('spinbutton', { name: 'MaxFiles', exact: true }).fill('1');
            await pick([first, second]);
            await page.getByText('Слишком много файлов', { exact: true }).waitFor();
            await page.getByRole('spinbutton', { name: 'MaxFileSize · байты', exact: true }).fill('100');
            await pick([first]);
            await page.getByText('Файл превышает допустимый размер', { exact: true }).waitFor();
            await page.getByRole('button', { name: 'Сбросить input', exact: true }).click();
            await page.getByRole('alert').waitFor({ state: 'hidden' });
            await page.getByRole('spinbutton', { name: 'MaxFileSize · байты', exact: true }).fill('10485760');
            await pick([first]);
            await page.getByText('file-input-first.svg', { exact: true }).waitFor();
        });
        await test('Custom trigger Enter, disabled and skeleton', async () => {
            await page.getByRole('checkbox', { name: /Custom trigger|Свой триггер/ }).check();
            await pick([first], true, true);
            await page.getByText('file-input-first.svg', { exact: true }).waitFor();
            assert.equal(await input.getAttribute('tabindex'), '-1');
            await page.getByRole('checkbox', { name: 'Disabled', exact: true }).check();
            await page.waitForFunction(() => document.querySelector('input[type=file]').disabled);
            assert.equal(await page.getByRole('button', { name: 'Выбрать файлы', exact: true }).isEnabled(), false);
            assert.equal(await page.getByRole('button', { name: 'Удалить файл', exact: true }).isEnabled(), false);
            await page.getByRole('checkbox', { name: 'Disabled', exact: true }).uncheck();
            await page.getByRole('checkbox', { name: 'Skeleton', exact: true }).check();
            await page.waitForFunction(() => document.querySelector('.admiral-file-input-control__dropzone').getAttribute('aria-busy') === 'true');
            assert.equal(await input.isEnabled(), false);
        });
        await context.close();
    }
} finally { await browser.close(); }
await writeFile(`${output}/results.json`, JSON.stringify(results, null, 2));
console.log(results);
// Native OS drag/drop remains a manual check; setting FileList is not proof of OS drag/drop.
if (results.some(result => !result.passed)) process.exitCode = 1;
