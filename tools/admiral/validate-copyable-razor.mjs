#!/usr/bin/env node
// Compile the code actually displayed by CopyableRazorExample against packaged Admiral DLLs.
// Usage: node tools/admiral/validate-copyable-razor.mjs [--url http://localhost:5072] [--output /tmp/admiral-snippets] [--limit 5]
import { createRequire } from 'node:module';
import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { promisify } from 'node:util';
import { readFile, readdir, mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const run = promisify(execFile);
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const pagesDir = path.join(root, 'samples/AdmiralConsumerSmoke/Pages');
const projectDir = path.join(root, 'samples/AdmiralConsumerValidation');
const project = path.join(projectDir, 'AdmiralConsumerValidation.csproj');
const snippetFile = path.join(projectDir, 'ValidationSnippet.razor');
const args = process.argv.slice(2);
const option = (name, fallback) => {
  const index = args.indexOf(name);
  return index < 0 ? fallback : args[index + 1];
};
const base = option('--url', process.env.SMOKE_URL || 'http://localhost:5072');
const output = path.resolve(option('--output', '/tmp/admiral-copyable-razor'));
const limit = Number(option('--limit', '0'));
const cache = path.join(output, 'nuget-packages');
const env = { ...process.env, NUGET_PACKAGES: cache, DOTNET_CLI_TELEMETRY_OPTOUT: '1' };
const hash = value => createHash('sha256').update(value).digest('hex');
const command = async (program, commandArgs, cwd = projectDir) => {
  try {
    const result = await run(program, commandArgs, { cwd, env, maxBuffer: 16 * 1024 * 1024 });
    return { code: 0, output: result.stdout + result.stderr };
  } catch (error) {
    return { code: error.code ?? 1, output: (error.stdout || '') + (error.stderr || '') };
  }
};

await mkdir(output, { recursive: true });
const pages = [];
for (const file of await readdir(pagesDir)) {
  if (!file.endsWith('.razor')) continue;
  const source = await readFile(path.join(pagesDir, file), 'utf8');
  const routes = [...source.matchAll(/@page\s+"([^"]+)"/g)].map(match => match[1]);
  if (routes.length) pages.push({ file, routes, copyCalls: (source.match(/<CopyableRazorExample\b/g) || []).length });
}
pages.sort((a, b) => a.file.localeCompare(b.file));

const browser = await chromium.launch({ headless: true });
const occurrences = [];
const scanErrors = [];
try {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  await context.addCookies([{ name: '.AspNetCore.Culture', value: 'c=en|uic=en', url: base }]);
  const page = await context.newPage();
  page.setDefaultTimeout(10000);
  const capture = async (file, route, tab) => {
    const examples = await page.locator('.docs-code-example').evaluateAll(elements => elements.map(element => ({
      title: element.querySelector('h3')?.textContent?.trim() || 'Razor',
      code: element.querySelector('pre code')?.textContent || ''
    })));
    for (const example of examples) {
      if (example.code.trim()) occurrences.push({ file, route, tab, ...example });
    }
  };
  for (const entry of pages) {
    // Aliases share the same component and copied code; one route is sufficient.
    const route = entry.routes[0];
    try {
      const response = await page.goto(new URL(route, base).href, { waitUntil: 'domcontentloaded' });
      if (response?.status() !== 200) throw new Error(`HTTP ${response?.status()}`);
      await page.locator('body').waitFor();
      // Static prerender can show code before the interactive circuit accepts tab clicks.
      await page.waitForTimeout(600);
      await capture(entry.file, route, 'initial');
      for (const tab of await page.locator('.component-doc-tabs [data-doc-tab]').evaluateAll(elements =>
        elements.map(element => element.getAttribute('data-doc-tab')))) {
        await page.locator(`.component-doc-tabs [data-doc-tab="${tab}"]`).first().click();
        await page.waitForFunction(tab => document.querySelector(`.component-doc-tabs [data-doc-tab="${tab}"]`)
          ?.getAttribute('aria-pressed') === 'true', tab);
        await capture(entry.file, route, tab);
      }
      console.log(`Scanned ${route}`);
    } catch (error) {
      scanErrors.push({ file: entry.file, route, error: error.message });
      console.error(`SCAN ERROR ${route}: ${error.message}`);
    }
  }
  await context.close();
} finally {
  await browser.close();
}

const unique = new Map();
for (const occurrence of occurrences) {
  const id = hash(occurrence.code).slice(0, 12);
  if (!unique.has(id)) unique.set(id, { id, code: occurrence.code, occurrences: [] });
  unique.get(id).occurrences.push({ file: occurrence.file, route: occurrence.route, tab: occurrence.tab, title: occurrence.title });
}
const snippets = [...unique.values()];
for (const entry of pages.filter(entry => entry.copyCalls > 0)) {
  if (!occurrences.some(occurrence => occurrence.file === entry.file)) {
    scanErrors.push({ file: entry.file, route: entry.routes[0], error: 'CopyableRazorExample exists in source but no rendered snippet was captured' });
  }
}
await writeFile(path.join(output, 'inventory.json'), JSON.stringify({ pages, scanErrors, occurrences, snippets }, null, 2));
console.log(`Inventory: ${occurrences.length} displayed examples, ${snippets.length} unique snippets, ${scanErrors.length} scan errors`);

// A separate package directory makes stale globally cached Admiral DLLs impossible.
const restore = await command('dotnet', ['restore', project, '--configfile', path.join(projectDir, 'NuGet.config'), '--force', '-v:q']);
if (restore.code !== 0) throw new Error(`Isolated restore failed:\n${restore.output}`);
const assets = JSON.parse(await readFile(path.join(projectDir, 'obj/project.assets.json'), 'utf8'));
if ((await readFile(project, 'utf8')).includes('ProjectReference')) {
  throw new Error('Validation consumer must use PackageReference only');
}
const packageFolders = Object.keys(assets.packageFolders || {});
if (packageFolders.length !== 1 || path.resolve(packageFolders[0]) !== path.resolve(cache)) {
  throw new Error(`Restore did not use the isolated NuGet cache: ${packageFolders.join(', ')}`);
}
for (const [name, version] of Object.entries({
  'AdmiralRadzen.Components': '1.2.2', 'AdmiralRadzen.Icons': '1.0.3', 'AdmiralRadzen.Themes': '1.1.2'
})) {
  const nupkg = path.join(root, 'artifacts/packages/admiral', `${name}.${version}.nupkg`);
  const cached = path.join(cache, name.toLowerCase(), version, `${name.toLowerCase()}.${version}.nupkg`);
  if (hash(await readFile(nupkg)) !== hash(await readFile(cached))) {
    throw new Error(`${name} ${version} in isolated cache differs from local package`);
  }
}
if (assets.projectFileDependencyGroups?.['net10.0']?.some(dependency => dependency.startsWith('AdmiralRadzen.Components >=')) !== true) {
  throw new Error('Consumer did not restore AdmiralRadzen.Components as a PackageReference');
}

const originalSnippet = await readFile(snippetFile, 'utf8');
const results = [];
try {
  await writeFile(snippetFile, '<p>Package consumer baseline</p>\n');
  const baseline = await command('dotnet', ['build', project, '--no-restore', '-nologo', '-v:q']);
  if (baseline.code !== 0) throw new Error(`Package consumer baseline failed:\n${baseline.output}`);
  for (const snippet of limit > 0 ? snippets.slice(0, limit) : snippets) {
    const title = snippet.occurrences[0].title;
    const kind = /Program\.cs|Host page|Theme and icon assets/i.test(title) ||
      /^\s*(<link\b|<script\b|builder\.|using Microsoft\.)/.test(snippet.code)
      ? 'host-or-setup' : 'razor';
    if (kind !== 'razor') {
      results.push({ id: snippet.id, status: 'requires-host-context', kind, occurrences: snippet.occurrences });
      continue;
    }
    await writeFile(snippetFile, snippet.code + '\n');
    const built = await command('dotnet', ['build', project, '--no-restore', '-nologo', '-v:q']);
    const diagnostics = [...new Set(built.output.split(/\r?\n/).filter(line => /\berror (?:CS|RZ)\d+\b/.test(line)))]
      .map(line => line.replaceAll(projectDir + path.sep, ''));
    const warnings = [...new Set(built.output.split(/\r?\n/).filter(line => /\bwarning (?:CS|RZ)\d+\b/.test(line)))]
      .map(line => line.replaceAll(projectDir + path.sep, ''));
    const codes = diagnostics.map(line => /\berror ((?:CS|RZ)\d+)\b/.exec(line)?.[1]).filter(Boolean);
    const missingSymbols = [...new Set(diagnostics.filter(line => /\berror (?:CS0103|CS0246)\b/.test(line))
      .map(line => /["“'‘]([^"”'’]+)["”'’]/.exec(line)?.[1]).filter(Boolean))];
    const onlyMissingContext = codes.length && codes.every(code => ['CS0103', 'CS0246'].includes(code)) &&
      !missingSymbols.some(symbol => ['True', 'False'].includes(symbol));
    const status = built.code === 0 ? (warnings.some(line => /\bwarning RZ10012\b/.test(line)) ? 'compiler-warning' : 'self-contained') :
      onlyMissingContext ? 'requires-component-context' : 'compiler-failure';
    results.push({ id: snippet.id, status, kind, occurrences: snippet.occurrences, missingSymbols, diagnostics, warnings,
      output: built.code === 0 ? undefined : built.output.trim() });
    console.log(`${snippet.id} ${status} ${snippet.occurrences[0].route} ${title}`);
  }
} finally {
  await writeFile(snippetFile, originalSnippet);
}
const counts = Object.fromEntries([...new Set(results.map(result => result.status))]
  .map(status => [status, results.filter(result => result.status === status).length]));
await writeFile(path.join(output, 'results.json'), JSON.stringify({
  base, isolatedNuGetCache: cache, packageFolders, counts, scanErrors, results
}, null, 2));
console.log(`Results: ${JSON.stringify(counts)}; full diagnostics: ${path.join(output, 'results.json')}`);
if (scanErrors.length || results.some(result => ['compiler-failure', 'compiler-warning'].includes(result.status))) process.exitCode = 1;
