import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import path from 'node:path';
if (!process.argv[2]) throw Error('Pass the unchanged Bar pointer/keyboard capture JSON.');
const bytes = await readFile(process.argv[2]), capture = JSON.parse(bytes), dir = path.dirname(path.resolve(process.argv[2]));
if (capture.sourceCommit !== 'a86059013cd59684ce452849278ea81e734f2066' || capture.sourceSha256 !== 'dcd8c302e50d0b8255207a57603999a5498cf4361034021d71aab96cd6f65235' || capture.react !== '19.2.4' || capture.recharts !== '3.8.1' || capture.states.length !== 6 || capture.errors.length) throw Error('Frozen pointer-priority capture differs.');
const source = await readFile(path.resolve(dir, '../line-chart-browser/frozen-source/src/components/bar-chart.tsx'));
if (createHash('sha256').update(source).digest('hex') !== capture.sourceSha256) throw Error('Frozen Bar source differs.');
const lock = JSON.parse(await readFile(path.resolve(dir, '../chart-theme-reference/bleecker-package-lock.json'), 'utf8'));
const meta = JSON.parse(await readFile(path.join(dir, 'build-meta.json'), 'utf8')), packages = new Map();
for (const input of Object.keys(meta.inputs)) {
 const full = path.resolve(input).replaceAll('\\', '/'), at = full.lastIndexOf('/node_modules/'); if (at < 0) continue;
 const suffix = full.slice(at + 14).split('/'), name = suffix[0].startsWith('@') ? suffix.slice(0, 2).join('/') : suffix[0], root = full.slice(0, at + 14) + name;
 if (packages.has(root)) continue;
 const manifest = JSON.parse(await readFile(path.join(root, 'package.json'), 'utf8')), first = full.indexOf('/node_modules/');
 const actualLock = JSON.parse(await readFile(full.slice(0, first + 14) + '.package-lock.json', 'utf8')), entry = actualLock.packages[root.slice(first + 1)];
 if (!entry || entry.version !== manifest.version || !Object.entries(lock.packages).some(([key, value]) => (key === 'node_modules/' + name || key.endsWith('/node_modules/' + name)) && value.version === entry.version && value.integrity === entry.integrity)) throw Error('Bundled runtime differs: ' + root);
 packages.set(root, { name, version: entry.version, integrity: entry.integrity });
}
if (JSON.stringify([...packages.values()].sort((a, b) => a.name.localeCompare(b.name))) !== JSON.stringify(capture.bundledRuntime)) throw Error('Capture runtime does not match its actual bundle.');
if (capture.bundledRuntime.filter(p => p.name === 'react').length !== 1) throw Error('One canonical React runtime is required.');
await writeFile(new URL('../tests/Reference/ChartPointerPriority.json', import.meta.url), bytes);
console.log(JSON.stringify({ states: capture.states.length, sha256: createHash('sha256').update(bytes).digest('hex') }));
