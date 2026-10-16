import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => { const index = process.argv.indexOf(name); assert.ok(index >= 0 && process.argv[index + 1], `Pass ${name} <path>.`); return path.resolve(process.argv[index + 1]); };
const sourceDirectory = option('--source'), nativeDirectory = option('--native'), output = option('--output');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const referenceBytes = await fs.readFile(new URL('../tests/Reference/DataTablePagination.json', import.meta.url));
const reference = JSON.parse(referenceBytes), referenceSha256 = hash(referenceBytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory, 'reference.json'))), referenceSha256);
for (const [field, file] of [['captureScriptSha256', 'capture-data-table-pagination.cjs'], ['clientSha256', 'data-table-pagination-client.tsx'], ['casesScriptSha256', 'data-table-pagination-cases.cjs']])
  assert.equal(reference[field], hash(await fs.readFile(new URL(`./reference/${file}`, import.meta.url))));
const runtime = await read(path.join(nativeDirectory, 'result.json'));
assert.equal(runtime.passed, true); assert.equal(runtime.component, 'data-table-pagination-source');
assert.equal(runtime.environment.displayScale, 1); assert.equal(runtime.environment.textScale, 1);
assert.equal(runtime.environment.animationsEnabled, false); assert.equal(runtime.environment.highContrast, false);
const pairs = [];
for (const scheme of ['Light', 'Dark']) {
  const report = await read(path.join(nativeDirectory, `data-table-pagination-source-${scheme}.json`));
  assert.equal(report.checkedGatesPassed, true); assert.equal(report.referenceSha256, referenceSha256);
  const expected = reference.cases.filter(item => item.scheme === scheme).flatMap(item => item.snapshots.map((point, index) => ({id: item.id, viewportWidth: item.viewportWidth, step: index + 1, ...point})));
  assert.equal(expected.length, report.reports.length);
  for (const [index, point] of expected.entries()) {
    const actual = report.reports[index]; assert.equal(actual.id, point.id); assert.equal(actual.step, point.step); assert.equal(actual.action, point.action); assert.deepEqual(actual.errors, []);
    const sourceBytes = await fs.readFile(path.join(sourceDirectory, point.file)); assert.equal(hash(sourceBytes), point.sha256);
    const browser = readRgbaPng(sourceBytes), nativeBytes = await fs.readFile(path.join(nativeDirectory, actual.file)), native = readRgbaPng(nativeBytes), crop = actual.surface;
    assert.equal(native.width, point.viewportWidth);
    for (const value of Object.values(crop)) assert.equal(value, Math.round(value), 'Capture crop must use actual integer pixels.');
    assert.ok(crop.x >= 0 && crop.y >= 0 && crop.x + crop.width <= native.width && crop.y + crop.height <= native.height);
    let differentPixels = 0, missingPixels = 0, maxChannel = 0;
    for (let y = 0; y < Math.max(browser.height, crop.height); y++) for (let x = 0; x < Math.max(browser.width, crop.width); x++) {
      const inSource = x < browser.width && y < browser.height, inNative = x < crop.width && y < crop.height;
      if (!inSource && !inNative) continue;
      if (!inSource || !inNative) { differentPixels++; missingPixels++; continue; }
      const sourceOffset = (y * browser.width + x) * 4, nativeOffset = ((y + crop.y) * native.width + x + crop.x) * 4;
      let changed = false;
      for (let channel = 0; channel < 4; channel++) { const delta = Math.abs(browser.rgba[sourceOffset + channel] - native.rgba[nativeOffset + channel]); changed ||= delta !== 0; maxChannel = Math.max(maxChannel, delta); }
      if (changed) differentPixels++;
    }
    pairs.push({id: point.id, scheme, action: point.action, sourceFile: point.file, sourceSha256: point.sha256, sourceSize: [browser.width, browser.height], nativeFile: actual.file, nativeSha256: hash(nativeBytes), crop, differentPixels, missingPixels, maxChannel, layoutDifferences: actual.layoutDifferences, masks: 0, tolerance: 0});
  }
}
assert.equal(pairs.length, 132);
const exactPairs = pairs.filter(pair => pair.differentPixels === 0).length, passed = exactPairs === pairs.length;
const summary = {pairs: pairs.length, exactPairs, passed, differentPixels: pairs.reduce((sum, pair) => sum + pair.differentPixels, 0), missingPixels: pairs.reduce((sum, pair) => sum + pair.missingPixels, 0), layoutDifferences: pairs.reduce((sum, pair) => sum + pair.layoutDifferences.length, 0)};
await fs.writeFile(output, JSON.stringify({format: 'orchard-data-table-pagination-pixels-v1', referenceSha256, ...summary, nativeComponentAccepted: false, masks: 0, tolerance: 0, pairs}, null, 2) + '\n');
console.log(JSON.stringify(summary)); if (!passed) process.exitCode = 1;
