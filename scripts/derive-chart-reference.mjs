import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import path from 'node:path';

if (!process.argv[2]) throw Error('Pass the unchanged Chart browser capture JSON.');
const bytes = await readFile(process.argv[2]), capture = JSON.parse(bytes), dir = path.dirname(path.resolve(process.argv[2]));
if (capture.sourceCommit !== 'a86059013cd59684ce452849278ea81e734f2066' || capture.sourceSha256 !== 'eacc1a099bf0957734ddbffcae03f28a4a087f7c3ba676812ba42094d3d21ece' || capture.react !== '19.2.4' || capture.recharts !== '3.8.1' || capture.cases.length !== 94 || new Set(capture.cases.map(c => c.id + '/' + c.scheme)).size !== 94) throw Error('Frozen Chart capture differs.');
if (capture.bundledRuntime.filter(p => p.name === 'react').length !== 1 || capture.bundledRuntime.find(p => p.name === 'react').version !== '19.2.4') throw Error('Capture has duplicate or unpinned React.');
const source = await readFile(path.resolve(dir, '../line-chart-browser/frozen-source/src/components/chart.tsx'));
if (createHash('sha256').update(source).digest('hex') !== capture.sourceSha256) throw Error('Frozen Chart source differs.');
const lock = JSON.parse(await readFile(path.resolve(dir, '../chart-theme-reference/bleecker-package-lock.json'), 'utf8'));
const meta = JSON.parse(await readFile(path.join(dir, 'build-meta.json'), 'utf8')), packages = new Map();
for (const input of Object.keys(meta.inputs)) {
 const full = path.resolve(input).replaceAll('\\', '/'), at = full.lastIndexOf('/node_modules/');
 if (at < 0) continue;
 const suffix = full.slice(at + 14).split('/'), name = suffix[0].startsWith('@') ? suffix.slice(0, 2).join('/') : suffix[0];
 const root = full.slice(0, at + 14) + name; if (packages.has(root)) continue;
 const manifest = JSON.parse(await readFile(path.join(root, 'package.json'), 'utf8')), first = full.indexOf('/node_modules/');
 const actualLock = JSON.parse(await readFile(full.slice(0, first + 14) + '.package-lock.json', 'utf8')), entry = actualLock.packages[root.slice(first + 1)];
 if (!entry || entry.version !== manifest.version || !Object.entries(lock.packages).some(([key, value]) => (key === 'node_modules/' + name || key.endsWith('/node_modules/' + name)) && value.version === entry.version && value.integrity === entry.integrity)) throw Error('Bundled runtime differs: ' + root);
 packages.set(root, { name, version: entry.version, integrity: entry.integrity });
}
if (JSON.stringify([...packages.values()].sort((a, b) => a.name.localeCompare(b.name))) !== JSON.stringify(capture.bundledRuntime)) throw Error('Capture runtime does not match its actual bundle.');
for (const sample of capture.cases) {
 if (sample.errors.length || !['Light', 'Dark'].includes(sample.scheme) || !capture.cases.some(c => c.id === sample.id && c.scheme !== sample.scheme) || sample.normal.surface.role !== 'application' || sample.normal.surface.tabindex !== '0' || sample.normal.legend.length) throw Error('Source case failed or changed its interaction surface.');
 sample.states = sample.states.map(state => ({ ...state, state: { tooltip: state.state.tooltip, dots: state.state.dots, cursor: state.state.cursor, focused: state.state.focused } }));
}
capture.fullCaptureSha256 = createHash('sha256').update(bytes).digest('hex');
const result = JSON.stringify(capture, null, 2) + '\n';
await writeFile(new URL('../tests/Reference/ChartLayout.json', import.meta.url), result);
console.log(JSON.stringify({ cases: capture.cases.length, sha256: createHash('sha256').update(result).digest('hex') }));
