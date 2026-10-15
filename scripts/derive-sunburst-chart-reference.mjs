import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import path from 'node:path';
if (!process.argv[2]) throw Error('Pass the unchanged Sunburst capture JSON.');
const capture = JSON.parse(await readFile(process.argv[2], 'utf8'));
const dir = path.dirname(path.resolve(process.argv[2]));
if (capture.sourceCommit !== 'a86059013cd59684ce452849278ea81e734f2066' || capture.sourceSha256 !== '8bd60a556edd2feac675c9f473ac1895639948936e8cc42607cf3b3c45a2aa3a' || capture.react !== '19.2.4' || capture.recharts !== '3.8.1' || capture.cases.length !== 94 || new Set(capture.cases.map(c => c.id + '/' + c.scheme)).size !== 94) throw Error('Frozen Sunburst capture differs.');
if (capture.bundledRuntime.filter(p => p.name === 'react').length !== 1 || capture.bundledRuntime.find(p => p.name === 'react').version !== '19.2.4') throw Error('Capture has duplicate or unpinned React.');
const lock = JSON.parse(await readFile(path.resolve(dir, '../chart-theme-reference/bleecker-package-lock.json'), 'utf8'));
const meta = JSON.parse(await readFile(path.join(dir, 'build-meta.json'), 'utf8'));
const packages = new Map();
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
const runtime = [...packages.values()].sort((a, b) => a.name.localeCompare(b.name));
if (JSON.stringify(runtime) !== JSON.stringify(capture.bundledRuntime)) throw Error('Capture runtime does not match its actual bundle.');
const errors = JSON.parse(await readFile(path.join(dir, 'errors.json'), 'utf8'));
if (errors.sourceSha256 !== capture.sourceSha256 || errors.cases.length !== 4 || !errors.cases.every(c => c.errors.length > 0) || !errors.cases.some(c => c.id === 'cycle' && c.errors[0].name === 'RangeError')) throw Error('Source invalid-tree evidence differs.');
capture.errorCases = errors.cases;
for (const sample of capture.cases) {
 if (sample.errors.length || !['Light', 'Dark'].includes(sample.scheme) || !capture.cases.some(c => c.id === sample.id && c.scheme !== sample.scheme)) throw Error('Source case is missing or failed.');
 if (sample.normal.interactive.length || sample.normal.surface.role || sample.normal.surface.tabindex) throw Error('Unexpected source interaction surface.');
 for (const state of sample.states) {
  if (state.state.tooltip.length) throw Error('Sunburst unexpectedly rendered inspection.');
  state.state = { tooltip: state.state.tooltip, focused: state.state.focused, paths: state.state.paths.map(p => ({ attributes:p.attributes, fill:p.fill, stroke:p.stroke, opacity:p.opacity })), labels: state.state.labels.map(l => ({ text:l.text, attributes:l.attributes })) };
 }
}
const bytes = JSON.stringify(capture, null, 2) + '\n';
await writeFile(new URL('../tests/Reference/SunburstChartLayout.json', import.meta.url), bytes);
console.log(JSON.stringify({ cases:capture.cases.length, sha256:createHash('sha256').update(bytes).digest('hex') }));
