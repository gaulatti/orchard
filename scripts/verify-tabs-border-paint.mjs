import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {cssRgba} from './reference/css-paint.mjs';

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const option = key => {
  const at = process.argv.indexOf(key);
  assert(at >= 0 && process.argv[at + 1], key);
  return process.argv[at + 1];
};

export async function auditTabsBorderPaint(folder, hostAssembly = 'PackageConsumer.dll') {
  assert(['PackageConsumer.dll', 'Orchard.Catalog.dll'].includes(hostAssembly));
  const referenceBytes = await fs.readFile('tests/Reference/Tabs.json');
  const reference = JSON.parse(referenceBytes);
  const nativeBytes = await fs.readFile(path.join(folder, 'tabs-reference.json'));
  const native = JSON.parse(nativeBytes);
  const result = await read(path.join(folder, 'result.json'));
  const exit = await read(path.join(folder, 'process-exit.json'));
  const geometry = await read(path.join(folder, 'metadata-audit.json'));
  assert(geometry.passed && geometry.frames === 44 && geometry.maximumGeometryError === 0);
  assert(native.passed && native.hidden && native.frames.length === 44 && native.guards >= 200);
  assert.equal(native.referenceSha256, hash(referenceBytes));
  assert.equal(native.focus, 0); assert.equal(native.activations, 0);
  assert.equal(exit.exited, true); assert.equal(exit.exitCode, 0); assert.equal(exit.timedOut, false);
  for (const key of ['activateCalled', 'showCalled', 'focusCalled', 'hardwareInputSent']) assert.equal(result[key], false, key);
  assert.equal(result.environment.displayScale, 1); assert.equal(result.environment.textScale, 1);
  for (const [name, sha] of [['Orchard.dll', native.identity.librarySha256], [hostAssembly, native.identity.hostSha256]]) {
    assert.equal(hash(await fs.readFile(path.join(folder, 'app-identity', name))), sha);
  }

  const frames = [], roots = []; let comparedPixels = 0;
  for (const source of reference.cases.filter(row => row.file)) {
    const item = source.measurement.items.find(row => row.selected === 'true');
    if (!item || item.borderThickness[0] !== 1 || cssRgba(item.border)[3] === 0 || cssRgba(item.background)[3] === 0) continue;
    const sourceBytes = await fs.readFile(path.join('tests/Reference/Tabs', source.file));
    assert.equal(hash(sourceBytes), source.pngSha256);
    const nativeBytes = await fs.readFile(path.join(folder, source.file));
    const capture = native.pngs.find(row => row.file === source.file);
    assert(capture); assert.equal(hash(nativeBytes), capture.sha256);
    const expected = readRgbaPng(sourceBytes), actual = readRgbaPng(nativeBytes);
    assert.deepEqual([actual.width, actual.height], [expected.width, expected.height]);
    const actualItem = native.frames.find(row => row.id === source.spec.id && row.scheme === source.scheme).measurement.items[source.measurement.items.indexOf(item)];
    const brush = actualItem.border, alpha = brush.a / 255 * brush.opacity;
    const y = Math.round(item.box.y), inset = Math.ceil(parseFloat(item.radius)) + 2;
    assert(Math.abs(item.box.y - y) <= 1 / 64);
    let pixels = 0, mismatches = 0, maximumChannelDifference = 0, nativeLayeringMismatches = 0, maximumNativeLayeringDifference = 0;
    for (let x = Math.ceil(item.box.x) + inset; x < Math.floor(item.box.x + item.box.width) - inset; x++) {
      const at = (y * expected.width + x) * 4;
      // Only the straight, solid border span; corners and glyphs are outside
      // this paint boundary. Whole-frame raster diagnostics remain unchanged.
      assert([0, 1, 2, 3].every(channel => expected.rgba[at + channel] === expected.rgba[at - 4 + channel] && expected.rgba[at + channel] === expected.rgba[at + 4 + channel]));
      const difference = Math.max(...[0, 1, 2, 3].map(channel => Math.abs(actual.rgba[at + channel] - expected.rgba[at + channel])));
      // Independently check the native compositing boundary: border paint
      // must cover the tab's own fill, sampled below its straight border.
      // This does not turn a failed source-image comparison into a pass.
      const inside = ((y + 2) * actual.width + x) * 4;
      assert.equal(actual.rgba[inside + 3], 255);
      const layered = [brush.r, brush.g, brush.b].map((value, channel) => Math.round(value * alpha + actual.rgba[inside + channel] * (1 - alpha)));
      const layeringDifference = Math.max(...layered.map((value, channel) => Math.abs(actual.rgba[at + channel] - value)));
      nativeLayeringMismatches += layeringDifference > 1 ? 1 : 0;
      maximumNativeLayeringDifference = Math.max(maximumNativeLayeringDifference, layeringDifference);
      pixels++; mismatches += difference > 1 ? 1 : 0;
      maximumChannelDifference = Math.max(maximumChannelDifference, difference);
    }
    assert(pixels >= 8); comparedPixels += pixels;
    frames.push({file: source.file, pixels, mismatches, maximumChannelDifference, nativeLayeringMismatches, maximumNativeLayeringDifference});
    const root = source.measurement.root;
    if (root.borderThickness[0] === 1 && cssRgba(root.background)[3] > 0) {
      const actualRoot = native.frames.find(row => row.id === source.spec.id && row.scheme === source.scheme).measurement.root;
      const brush = actualRoot.border, alpha = brush.a / 255 * brush.opacity;
      const y = Math.round(root.box.y), inset = Math.ceil(parseFloat(root.radius)) + 2;
      let pixels = 0, sourceMismatches = 0, nativeLayeringMismatches = 0;
      for (let x = Math.ceil(root.box.x) + inset; x < Math.floor(root.box.x + root.box.width) - inset; x++) {
        const at = (y * actual.width + x) * 4, inside = ((y + 2) * actual.width + x) * 4;
        assert.equal(actual.rgba[inside + 3], 255);
        assert([0, 1, 2, 3].every(channel => expected.rgba[at + channel] === expected.rgba[at - 4 + channel] && expected.rgba[at + channel] === expected.rgba[at + 4 + channel]));
        const layered = [brush.r, brush.g, brush.b].map((value, channel) => Math.round(value * alpha + actual.rgba[inside + channel] * (1 - alpha)));
        nativeLayeringMismatches += layered.some((value, channel) => Math.abs(actual.rgba[at + channel] - value) > 1) ? 1 : 0;
        sourceMismatches += [0, 1, 2, 3].some(channel => Math.abs(actual.rgba[at + channel] - expected.rgba[at + channel]) > 1) ? 1 : 0;
        pixels++;
      }
      assert(pixels >= 8);
      roots.push({file: source.file, pixels, sourceMismatches, nativeLayeringMismatches});
    }
  }
  assert.equal(frames.length, 24, 'Both bordered variants, all sizes, themes and stretch settings are required');
  assert.equal(roots.length, 12, 'Every enclosed strip size, theme and stretch setting is required');
  const nativeTabBackgroundLayeringVerified = frames.every(row => row.nativeLayeringMismatches === 0);
  const nativeRootBackgroundLayeringVerified = roots.every(row => row.nativeLayeringMismatches === 0);
  return {passed: frames.every(row => row.mismatches === 0) && roots.every(row => row.sourceMismatches === 0), frames, roots, borderCases: frames.length, comparedPixels, mismatchingPixels: frames.reduce((sum, row) => sum + row.mismatches, 0), nativeTabBackgroundLayeringVerified, nativeRootBackgroundLayeringVerified, nativeBackgroundLayeringVerified: nativeTabBackgroundLayeringVerified && nativeRootBackgroundLayeringVerified, nativeBackgroundLayeringMismatches: [...frames, ...roots].reduce((sum, row) => sum + row.nativeLayeringMismatches, 0), rootBorderCases: roots.length, rootBorderPixels: roots.reduce((sum, row) => sum + row.pixels, 0), rootSourceMismatches: roots.reduce((sum, row) => sum + row.sourceMismatches, 0), reportSha256: hash(nativeBytes), librarySha256: native.identity.librarySha256, hostSha256: native.identity.hostSha256, channelTolerance: 1, fullRasterParityVerified: false, nativeComponentAccepted: false};
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const hostAt = process.argv.indexOf('--host-assembly');
  const audit = await auditTabsBorderPaint(option('--native'), hostAt < 0 ? undefined : process.argv[hostAt + 1]);
  await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n');
  console.log(JSON.stringify({...audit, frames: undefined}));
  process.exitCode = audit.passed ? 0 : 1;
}
