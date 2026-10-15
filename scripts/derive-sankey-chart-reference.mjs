import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { createHash } from 'node:crypto';
if (!process.argv[2]) throw Error('Pass the unchanged Sankey capture JSON.');
const file = path.resolve(process.argv[2]), dir = path.dirname(file);
const capture = JSON.parse(await readFile(file, 'utf8'));
if (capture.sourceCommit !== 'a86059013cd59684ce452849278ea81e734f2066' || capture.sourceSha256 !== '026b636fa2441ac68933be304bc25689be66efc8f6b669b524cc580c78af578e' || capture.react !== '19.2.4' || capture.recharts !== '3.8.1' || capture.cases.length !== 76 || new Set(capture.cases.map(c => c.id + '/' + c.scheme)).size !== 76) throw Error('Frozen Sankey capture differs.');
const lock = JSON.parse(await readFile(path.resolve(dir, '../chart-theme-reference/bleecker-package-lock.json'), 'utf8'));
const meta = JSON.parse(await readFile(path.join(dir, 'build-meta.json'), 'utf8'));
const packages = new Map();
for (const input of Object.keys(meta.inputs)) {
 const full = path.resolve(input).replaceAll('\\', '/'), at = full.lastIndexOf('/node_modules/');
 if (at < 0) continue;
 const suffix = full.slice(at + 14).split('/'), name = suffix[0].startsWith('@') ? suffix.slice(0, 2).join('/') : suffix[0];
 const root = full.slice(0, at + 14) + name;
 if (packages.has(root)) continue;
 const manifest = JSON.parse(await readFile(path.join(root, 'package.json'), 'utf8'));
 const first = full.indexOf('/node_modules/');
 const actualLock = JSON.parse(await readFile(full.slice(0, first + 14) + '.package-lock.json', 'utf8'));
 const entry = actualLock.packages[root.slice(first + 1)];
 if (!entry || entry.version !== manifest.version || !Object.entries(lock.packages).some(([key, v]) => (key === 'node_modules/' + name || key.endsWith('/node_modules/' + name)) && v.version === entry.version && v.integrity === entry.integrity)) throw Error('Bundled runtime differs: ' + root);
 packages.set(root, { name, version: entry.version, integrity: entry.integrity });
}
const react = [...packages.entries()].filter(([, value]) => value.name === 'react');
if (react.length !== 1 || react[0][1].version !== '19.2.4') throw Error('Source capture must use one pinned React runtime.');
capture.bundledRuntime = [...packages.values()].sort((a, b) => a.name.localeCompare(b.name));
capture.errorCases = JSON.parse(await readFile(path.join(dir, 'errors.json'), 'utf8')).cases;
if (capture.errorCases.length !== 5 || capture.errorCases.filter(c => c.errors.length).some(c => c.id !== 'rooted-cycle') || !capture.errorCases.some(c => c.id === 'rooted-cycle' && c.errors[0] === 'Maximum call stack size exceeded')) throw Error('Sankey error probes differ.');
for (const sample of [...capture.cases, ...capture.errorCases]) {
 if (capture.cases.includes(sample) && (sample.errors.length || !['Light', 'Dark'].includes(sample.scheme) || !capture.cases.some(c => c.id === sample.id && c.scheme !== sample.scheme))) throw Error('Invalid source case.');
 delete sample.normal.tooltipHtml;
 for (const state of sample.states) {
  const { tooltip, tooltipVisible, tooltipBox, tooltipLines, cursor, focused, links } = state.state;
  if (tooltipVisible && tooltipLines.some(l => l.text.trim() && !l.rendered)) throw Error('Unsettled source tooltip.');
  state.state = { tooltip, tooltipVisible, tooltipBox, tooltipLines, cursor, focused, links: links.map(link => ({ opacity: link.opacity, fillOpacity: link.fillOpacity })) };
 }
}
const bytes = JSON.stringify(capture, null, 2) + '\n';
await writeFile(new URL('../tests/Reference/SankeyChartLayout.json', import.meta.url), bytes);
console.log(JSON.stringify({ cases: capture.cases.length, errorCases: capture.errorCases.length, runtimePackages: packages.size, sha256: createHash('sha256').update(bytes).digest('hex') }));
