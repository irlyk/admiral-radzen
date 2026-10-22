// Layout inventory, not a component-parity verdict. Run against the live smoke app.
import { createRequire } from 'node:module';
import { readdir, readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const output = path.resolve(process.env.AUDIT_OUTPUT || path.join(root, 'artifacts/smoke-layout-audit'));
const baseURL = process.env.SMOKE_URL || 'http://localhost:5072';
const widths = (process.env.AUDIT_WIDTHS || '1440,768').split(',').map(Number);
const routeFilter = process.env.AUDIT_ROUTES?.split(',');
// A restarted server may still be binding its port; do not turn that race into 40 false failures.
for (let attempt = 0; ; attempt++) {
    try {
        const response = await fetch(baseURL, { signal: AbortSignal.timeout(5000) });
        if (response.ok) break;
    } catch { /* Retry only the initial readiness probe. */ }
    if (attempt === 19) throw new Error(`Smoke server is not ready at ${baseURL}`);
    await new Promise(resolve => setTimeout(resolve, 250));
}
const routes = [];
for (const file of await readdir(path.join(root, 'samples/AdmiralConsumerSmoke/Pages'))) {
    if (!file.endsWith('.razor')) continue;
    const source = await readFile(path.join(root, 'samples/AdmiralConsumerSmoke/Pages', file), 'utf8');
    const route = source.match(/@page "([^"]+)"/)?.[1];
    if (route && (!routeFilter || routeFilter.includes(route))) routes.push(route);
}
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {}) });
const results = [];
try {
    for (const theme of ['admiral', 'admiral-dark']) {
        for (const width of widths) {
            for (const culture of ['ru', 'en']) {
                const context = await browser.newContext({ viewport: { width, height: 1000 } });
                await context.addCookies([
                    { name: 'RadzenAdmiralTheme', value: theme, url: baseURL },
                    { name: '.AspNetCore.Culture', value: `c=${culture}|uic=${culture}`, url: baseURL }
                ]);
                const page = await context.newPage();
                page.setDefaultTimeout(5000);
                for (const route of routes.sort()) {
                    const result = { theme, width, culture, route, errors: [], tabs: [] };
                    const onError = error => result.errors.push(error.message);
                    page.on('pageerror', onError);
                    try {
                        await page.goto(baseURL + route);
                        await page.locator('.admiral-layout').waitFor();
                        await page.waitForTimeout(100);
                        const tabs = page.locator('.component-doc-tabs button');
                        const count = await tabs.count();
                        for (let index = 0; index < Math.max(1, count); index++) {
                            if (count) await tabs.nth(index).click();
                            await page.waitForTimeout(75);
                            result.tabs.push(await page.evaluate(() => {
                                const body = document.querySelector('.admiral-layout__body');
                                const rect = body.getBoundingClientRect();
                                const overflow = [...body.querySelectorAll('section, .admiral-component-playground, .admiral-component-controls, .component-doc-api, .component-doc-code')]
                                    .filter(e => e.getBoundingClientRect().width && e.getBoundingClientRect().right > rect.right + 2)
                                    .map(e => ({ tag: e.tagName, class: e.className, right: Math.round(e.getBoundingClientRect().right), limit: Math.round(rect.right) }));
                                return {
                                    tab: document.querySelector('.component-doc-tabs [aria-pressed="true"]')?.textContent,
                                    documentOverflow: document.documentElement.scrollWidth > document.documentElement.clientWidth + 2,
                                    overflow,
                                    errorOverlay: getComputedStyle(document.querySelector('#blazor-error-ui')).display !== 'none'
                                };
                            }));
                            if (['/tables', '/components/slider', '/components/typography'].includes(route)) {
                                await page.screenshot({ path: path.join(output, `${theme}-${width}-${culture}-${route.replaceAll('/', '_')}-${index}.png`) });
                            }
                        }
                    } catch (error) { result.errors.push(error.message); }
                    page.off('pageerror', onError);
                    results.push(result);
                    console.log(`${theme} ${width} ${culture} ${route}: ${result.errors.length} errors, ${result.tabs.reduce((n, t) => n + t.overflow.length, 0)} overflowing containers`);
                }
                await context.close();
            }
        }
    }
} finally {
    await writeFile(path.join(output, 'layout.json'), JSON.stringify(results, null, 2));
    await browser.close();
}
if (results.some(r => r.errors.length || r.tabs.some(t => t.documentOverflow || t.errorOverlay || t.overflow.length))) process.exitCode = 1;
