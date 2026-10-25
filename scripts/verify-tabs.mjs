import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {cssRgba} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => { const index = process.argv.indexOf(name); assert(index >= 0 && process.argv[index + 1]); return path.resolve(process.argv[index + 1]); };
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const source = await read(new URL('../tests/Reference/Tabs.json', import.meta.url)), nativeFolder = option('--native'), sourceFolder = option('--source');
const report = await read(path.join(nativeFolder, 'tabs-reference.json')), runtime = await read(path.join(nativeFolder, 'result.json'));
assert(report.passed && runtime.passed && runtime.component === 'tabs-reference');
assert.equal(runtime.environment.displayScale, 1); assert.equal(runtime.environment.textScale, 1);
assert.equal(report.frames.length, source.cases.length);
const key = row => [row.id ?? row.spec.id, row.scheme].join('/');
assert.deepEqual(report.frames.map(key).sort(), source.cases.map(key).sort());
const errors = [], raster = []; let maximumGeometryError = 0;
function near(actual, expected, context) {
  assert(Number.isFinite(actual) && Number.isFinite(expected), context);
  const error = Math.abs(actual - expected); maximumGeometryError = Math.max(maximumGeometryError, error);
  if (error >= .001) errors.push({context, actual, expected, error});
}
function box(actual, expected, context) { for (const field of ['x', 'y', 'width', 'height']) near(actual[field], expected[field], context + '/' + field); }
function thickness(actual, expected, context) {
  for (const [index, field] of ['Left', 'Top', 'Right', 'Bottom'].entries()) near(actual[field], expected[index], context + '/' + field);
}
function padding(css) {
  const values = css.split(/\s+/).map(parseFloat);
  const [top, right = top, bottom = top, left = right] = values;
  return [left, top, right, bottom];
}
function paint(actual, expected, context) {
  const [r, g, b, alpha] = cssRgba(expected), actualAlpha = actual.a / 255 * actual.opacity;
  if (Math.abs(actualAlpha - alpha) >= .000001 || (alpha > 0 && (actual.r !== r || actual.g !== g || actual.b !== b))) errors.push({context, actual, expected});
}
for (const reference of source.cases) {
  const native = report.frames.find(row => key(row) === key(reference)).measurement, wanted = reference.measurement, context = key(reference);
  box(native.root.box, wanted.root.box, context + '/root'); paint(native.root.background, wanted.root.background, context + '/root/background');
  if (wanted.root.borderThickness.some(value => value > 0)) paint(native.root.border, wanted.root.border, context + '/root/border');
  near(native.root.radius, parseFloat(wanted.root.radius), context + '/root/radius');
  assert.equal(native.items.length, wanted.items.length);
  for (let index = 0; index < native.items.length; index++) {
    const actual = native.items[index], expected = wanted.items[index], location = context + '/item-' + index;
    box(actual.box, expected.box, location); assert.equal(actual.text, expected.text);
    thickness(actual.borderThickness, expected.borderThickness, location + '/border-thickness');
    thickness(actual.padding, padding(expected.padding), location + '/padding');
    assert.equal(actual.Weight, Number(expected.fontWeight), location + '/font-weight');
    for (const field of ['foreground', 'background']) paint(actual[field], expected[field], location + '/' + field);
    if (expected.borderThickness.some(value => value > 0)) paint(actual.border, expected.border, location + '/border');
    near(actual.fontSize, parseFloat(expected.fontSize), location + '/font-size'); near(actual.lineHeight, parseFloat(expected.lineHeight), location + '/line-height');
    near(actual.radius, parseFloat(expected.radius), location + '/radius');
  }
  if (reference.file) {
    const bytes = await fs.readFile(path.join(sourceFolder, reference.file));
    assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'), reference.pngSha256);
    const expected = readRgbaPng(bytes), actual = readRgbaPng(await fs.readFile(path.join(nativeFolder, reference.file)));
    assert.deepEqual([actual.width, actual.height], [expected.width, expected.height]);
    let differingPixels = 0, absoluteRgbaError = 0, maximumChannelDifference = 0;
    for (let at = 0; at < actual.rgba.length; at += 4) {
      let different = false;
      for (let channel = 0; channel < 4; channel++) { const error = Math.abs(actual.rgba[at + channel] - expected.rgba[at + channel]); different ||= error !== 0; absoluteRgbaError += error; maximumChannelDifference = Math.max(maximumChannelDifference, error); }
      differingPixels += Number(different);
    }
    raster.push({file: reference.file, differingPixels, absoluteRgbaError, maximumChannelDifference});
  }
}
const audit = {passed: errors.length === 0, frames: report.frames.length, maximumGeometryError, errors, raster, sourceRasterParityVerified: raster.every(row => row.differingPixels === 0), masks: 0, pixelTolerance: 0, resampling: false, nativeComponentAccepted: false};
await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n');
console.log(JSON.stringify({...audit, raster: undefined, errors: errors.slice(0, 16)}));
assert(audit.passed, 'Tabs rendered contract differs.');
