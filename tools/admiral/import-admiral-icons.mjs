#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

function usage() {
  console.error('Usage: tools/admiral/import-admiral-icons.mjs <AdmiralDS/icons repo path>');
}

function slug(value) {
  return value
    .normalize('NFKD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/&/g, ' and ')
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .replace(/-+/g, '-')
    .toLowerCase();
}

function walkSvg(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      files.push(...walkSvg(fullPath));
      continue;
    }

    if (entry.isFile() && entry.name.toLowerCase().endsWith('.svg')) {
      files.push(fullPath);
    }
  }

  return files;
}

const sourceRootArg = process.argv[2];

if (!sourceRootArg) {
  usage();
  process.exit(2);
}

const repoRoot = process.cwd();
const sourceRoot = path.resolve(sourceRootArg, 'public', 'icons');
const targetRoot = path.join(repoRoot, 'Radzen.AdmiralIcons', 'icons');

if (!fs.existsSync(sourceRoot)) {
  console.error(`Admiral icons source folder not found: ${sourceRoot}`);
  process.exit(1);
}

const sourceFiles = walkSvg(sourceRoot).sort();
const planned = new Map();
const collisions = [];

for (const sourceFile of sourceFiles) {
  const relative = path.relative(sourceRoot, sourceFile);
  const parsed = path.parse(relative);
  const category = slug(path.dirname(relative));
  const name = slug(parsed.name);
  const targetRelative = path.join(category, `${name}.svg`);

  if (planned.has(targetRelative)) {
    collisions.push([targetRelative, planned.get(targetRelative), sourceFile]);
    continue;
  }

  planned.set(targetRelative, sourceFile);
}

if (collisions.length > 0) {
  console.error('Icon filename collisions after slug normalization:');
  for (const [target, first, second] of collisions) {
    console.error(`  ${target}`);
    console.error(`    ${first}`);
    console.error(`    ${second}`);
  }
  process.exit(1);
}

fs.rmSync(targetRoot, { recursive: true, force: true });
fs.mkdirSync(targetRoot, { recursive: true });

for (const [targetRelative, sourceFile] of planned) {
  const targetFile = path.join(targetRoot, targetRelative);
  fs.mkdirSync(path.dirname(targetFile), { recursive: true });
  fs.copyFileSync(sourceFile, targetFile);
}

const metadata = {
  source: 'https://github.com/AdmiralDS/icons',
  sourcePath: 'public/icons',
  importedAt: new Date().toISOString(),
  iconCount: planned.size,
  naming: 'lowercase ASCII slug paths, grouped by source category',
};

fs.writeFileSync(
  path.join(targetRoot, 'admiral-icons-source.json'),
  `${JSON.stringify(metadata, null, 2)}\n`,
);

console.log(`Imported ${planned.size} Admiral SVG icons into ${path.relative(repoRoot, targetRoot)}.`);
