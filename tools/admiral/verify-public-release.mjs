import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { spawnSync } from 'node:child_process';

const root = resolve(import.meta.dirname, '../..');
const packages = [
  ['Radzen.Admiral.Icons', 'Radzen.AdmiralIcons'],
  ['Radzen.Admiral.Themes', 'Radzen.AdmiralThemes'],
  ['Radzen.Admiral.Components', 'Radzen.AdmiralComponents'],
];
const run = (command, args, options = {}) => {
  const result = spawnSync(command, args, { cwd: root, encoding: 'utf8', ...options });
  assert.equal(result.status, 0, `${command} ${args.join(' ')} failed: ${result.stderr?.trim()}`);
  return result.stdout;
};
const xmlValue = (xml, name) => xml.match(new RegExp(`<${name}>([^<]+)</${name}>`))?.[1];
const member = (archive, name) => run('unzip', ['-p', archive, name]);

for (const name of ['README.md', 'README.en.md', 'LICENSE', 'THIRD_PARTY_NOTICES.md']) {
  assert.ok(existsSync(resolve(root, name)), `Missing required file: ${name}`);
}
assert.equal(run('git', ['status', '--porcelain=v1', '--untracked-files=normal']).trim(), '',
  'Release checkout must be clean');
const tracked = run('git', ['ls-files', '-z']).split('\0').filter(Boolean);
const forbidden = tracked.filter(name =>
  /(^|\/)(?:bin|obj|node_modules|artifacts|test-results|playwright-report)(\/|$)/i.test(name) ||
  /(^|\/)\.env(?:\..*)?$/i.test(name) && !name.endsWith('.env.example') ||
  /\.(?:pfx|p12|pem|key|nupkg|snupkg)$/i.test(name));
assert.deepEqual(forbidden, [], 'Tracked build output or sensitive files found');
run('git', ['diff', '--check']);

const license = readFileSync(resolve(root, 'LICENSE'), 'utf8');
const notices = readFileSync(resolve(root, 'THIRD_PARTY_NOTICES.md'), 'utf8');
for (const [id, directory] of packages) {
  const project = readFileSync(resolve(root, directory, `${directory}.csproj`), 'utf8');
  assert.equal(xmlValue(project, 'PackageId'), id, `${directory}: PackageId mismatch`);
  const version = xmlValue(project, 'Version');
  const licenseExpression = xmlValue(project, 'PackageLicenseExpression');
  assert.ok(version && licenseExpression, `${id}: version or license metadata missing`);
  const archive = resolve(root, 'artifacts/packages/admiral', `${id}.${version}.nupkg`);
  assert.ok(existsSync(archive), `${id}: package not built`);
  const members = run('unzip', ['-Z1', archive]).split('\n').filter(Boolean);
  for (const name of ['README.md', 'LICENSE', 'THIRD_PARTY_NOTICES.md', `${id}.nuspec`]) {
    assert.ok(members.includes(name), `${id}: missing ${name}`);
  }
  assert.equal(member(archive, 'LICENSE'), license, `${id}: LICENSE differs from source`);
  assert.equal(member(archive, 'THIRD_PARTY_NOTICES.md'), notices,
    `${id}: notices differ from source`);
  const nuspec = member(archive, `${id}.nuspec`);
  assert.equal(xmlValue(nuspec, 'id'), id, `${id}: packaged id mismatch`);
  assert.equal(xmlValue(nuspec, 'version'), version, `${id}: packaged version mismatch`);
  assert.ok(nuspec.includes(`<license type="expression">${licenseExpression}</license>`),
    `${id}: packaged license expression mismatch`);
  if (id === 'Radzen.Admiral.Themes') {
    for (const name of [
      'MaterialSymbolsOutlined.woff2', 'SourceSans3VF-Upright.ttf.woff2',
      'SourceSans3VF-Italic.ttf.woff2', 'cjk/NotoSansSC-variable.woff2',
      'cjk/NotoSansKR-variable.woff2', 'cjk/OFL.txt',
    ]) assert.ok(members.includes(`staticwebassets/fonts/${name}`), `${id}: missing ${name}`);
  }
  if (id === 'Radzen.Admiral.Icons') {
    for (const name of [
      'staticwebassets/admiral-radzen-icons.css',
      'staticwebassets/admiral-radzen-icons.js',
      'staticwebassets/admiral-icons/icons.txt',
    ]) assert.ok(members.includes(name), `${id}: missing ${name}`);
  }
  console.log(`${id} ${version}: PASS`);
}
console.log('Release source and package checks: PASS');
