#!/usr/bin/env node
// Select existing browser suites from real Razor composition and SCSS selectors.
import { execFileSync, spawnSync } from 'node:child_process';
import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const read = file => readFileSync(path.join(root, file), 'utf8');
const list = dir => readdirSync(path.join(root, dir)).map(name => `${dir}/${name}`);
const packageJson = JSON.parse(read('package.json'));
const listOnly = process.argv.includes('--list');
const suites = Object.keys(packageJson.scripts).filter(name => name.startsWith('test:browser:'));
const testFiles = new Map(suites.map(name => [name, packageJson.scripts[name].match(/tools\/admiral\/[^ ]+\.mjs/)?.[0]]));
const changed = process.argv.slice(2).filter(arg => !arg.startsWith('--'));
if (!changed.length) {
  const git = (...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' }).trim().split('\n').filter(Boolean);
  changed.push(...new Set([...git('diff', '--name-only', 'HEAD'), ...git('ls-files', '--others', '--exclude-standard')]));
}
const all = reason => {
  console.log(`Affected regression fallback: full browser suite (${reason})`);
  packageCheck();
  if (!listOnly) run('test:browser');
};
function packageCheck() {
  if (listOnly || !changed.some(file => file.startsWith('Radzen.AdmiralComponents/') || file.startsWith('Radzen.AdmiralThemes/'))) return;
  console.log('Local package changes: rebuild three packages and validate isolated NuGet consumer');
  const build = spawnSync('tools/admiral/build-admiral-packages.sh', ['--configuration', 'Release'], { cwd: root, stdio: 'inherit' });
  if (build.status !== 0) process.exit(build.status || 1);
  const consumer = spawnSync('node', ['tools/admiral/validate-consumer-integration.mjs'], { cwd: root, stdio: 'inherit' });
  if (consumer.status !== 0) process.exit(consumer.status || 1);
}
function run(name) {
  console.log(`Running ${name}`);
  const result = spawnSync('npm', ['run', name], { cwd: root, stdio: 'inherit', env: process.env });
  if (result.status !== 0) process.exit(result.status || 1);
}
if (!changed.length) all('no changed files; baseline run');
else {
  const componentFiles = list('Radzen.AdmiralComponents').filter(f => /\.razor$/.test(f));
  const components = new Map(componentFiles.map(file => [path.basename(file, '.razor'), file]));
  const pages = list('samples/AdmiralConsumerSmoke/Pages').filter(f => f.endsWith('.razor'));
  const markup = new Map([...componentFiles, ...pages].map(file => [file, read(file)]));
  const tags = source => new Set([...source.matchAll(/<\s*(Admiral[A-Za-z0-9]+|Radzen[A-Za-z0-9]+)/g)].map(m => m[1]));
  const direct = new Map([...markup].map(([file, source]) => [file, tags(source)]));
  const closure = (file, visited = new Set()) => {
    if (visited.has(file)) return new Set();
    visited.add(file);
    const found = new Set(direct.get(file) || []);
    for (const tag of [...found]) if (components.has(tag)) for (const inner of closure(components.get(tag), visited)) found.add(inner);
    return found;
  };
  const pageRoutes = new Map(pages.map(file => [file,
    [...read(file).matchAll(/@page\s+"([^"]+)"/g)].map(m => m[1])]));
  const suiteRoutes = new Map([...testFiles].map(([name, file]) => [name,
    file ? new Set([...read(file).matchAll(/\/(?:components\/[a-z-]+|regression\/[a-z-]+|phone-regression|pagination-regression|tables)/g)].map(m => m[0])) : new Set()]));
  const selected = new Map();
  const add = (suite, reason) => { if (!selected.has(suite)) selected.set(suite, new Set()); selected.get(suite).add(reason); };
  let uncertain = '';
  for (const file of changed) {
    if (file === 'package.json' || file === 'tools/admiral/run-affected-regressions.mjs') continue;
    const affectedPages = new Set();
    if (components.has(path.basename(file).replace(/\.razor(?:\.cs)?$/, '')) && file.startsWith('Radzen.AdmiralComponents/')) {
      const component = path.basename(file).replace(/\.razor(?:\.cs)?$/, '');
      for (const page of pages) if (closure(page).has(component)) affectedPages.add(page);
    } else if (file.startsWith('Radzen.AdmiralThemes/themes/') && file.endsWith('.scss')) {
      const css = read(file);
      const selectors = new Set([...css.matchAll(/\.((?:admiral|rz)-[a-z0-9_-]+)/g)].map(m => m[1]));
      if (!selectors.size || [...selectors].some(s => ['rz-dropdown', 'rz-dropdown-panel', 'rz-dropdown-item', 'rz-inputtext'].includes(s))) {
        uncertain = `${file}: shared Radzen selectors`;
        break;
      }
      for (const page of pages) {
        const sources = [page, ...[...closure(page)].map(name => components.get(name)).filter(Boolean)];
        if (sources.some(source => [...selectors].some(s => read(source).includes(s)))) affectedPages.add(page);
      }
    } else if (file.startsWith('samples/AdmiralConsumerSmoke/Pages/') && file.endsWith('.razor')) affectedPages.add(file);
    else if (file.startsWith('tools/admiral/') && testFiles.has([...testFiles].find(([, source]) => source === file)?.[0])) {
      add([...testFiles].find(([, source]) => source === file)[0], `${file}: test changed`);
      continue;
    } else if (file.startsWith('samples/AdmiralConsumerValidation/')) continue;
    else if (file.startsWith('docs/')) continue;
    else { uncertain = `${file}: dependency not classified`; break; }
    if (!affectedPages.size) { uncertain = `${file}: no dependent documentation route`; break; }
    console.log(`${file} affects ${[...affectedPages].map(page => path.basename(page, '.razor')).join(', ')} (Razor composition / SCSS selectors)`);
    for (const page of affectedPages) {
      const routes = pageRoutes.get(page) || [];
      // Repository utility pages reuse Select but have no component-specific suite;
      // Select's dedicated route exercises the same library path.
      if (routes.every(route => ['/coverage', '/icons'].includes(route))) continue;
      const matched = [...suiteRoutes].filter(([, used]) => routes.some(route => used.has(route)));
      if (!matched.length) { uncertain = `${file}: ${page} lacks mapped browser suite`; break; }
      for (const [suite] of matched) add(suite, `${file} → ${page} (Razor composition / route)`);
    }
    if (uncertain) break;
  }
  if (uncertain) {
    for (const [suite, reasons] of selected) console.log(`Detected ${suite}: ${[...reasons].join('; ')}`);
    all(uncertain);
  }
  else {
    packageCheck();
    if (!selected.size) console.log('No browser-dependent source changes');
    for (const [suite, reasons] of selected) {
      console.log(`${suite}: ${[...reasons].join('; ')}`);
      if (!listOnly) run(suite);
    }
  }
}
