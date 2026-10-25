import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {cssRgba} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]); };
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const native = option('--native'), sourceFolder = option('--source');
const reference = await read(new URL('../tests/Reference/DialogCloseButton.json', import.meta.url));
const result = await read(path.join(native, 'result.json')), report = await read(path.join(native, 'dialog-close.json'));
assert(result.passed && report.passed && result.component === 'dialog-close');
assert.equal(result.environment.displayScale, 1); assert.equal(result.environment.textScale, 1);
const key = row => row.id + '/' + row.scheme;
assert.deepEqual(report.frames.map(key).sort(), reference.cases.map(key).sort());
let maximumGeometryError = 0; const errors = [], raster = [];
function near(actual, expected, context) {
  assert(Number.isFinite(actual) && Number.isFinite(expected), context);
  const error = Math.abs(actual - expected); maximumGeometryError = Math.max(maximumGeometryError, error);
  if (error >= .001) errors.push({context, actual, expected, error});
}
function paint(actual, css, context) {
  const [r, g, b, alpha] = cssRgba(css);
  if (actual.r !== r || actual.g !== g || actual.b !== b || Math.abs(actual.a / 255 * actual.opacity - alpha) >= .000001) errors.push({context, actual, expected: css});
}
for (const row of reference.cases) {
  const actual = report.frames.find(frame => key(frame) === key(row)).measurement, expected = row.measurement, context = key(row);
  for (const subject of ['box', 'icon']) for (const field of ['x', 'y', 'width', 'height']) near(actual[subject][field], expected[subject][field], context + '/' + subject + '/' + field);
  for (const field of ['foreground', 'background', 'border']) paint(actual[field], expected[field], context + '/' + field);
  near(actual.radius, parseFloat(expected.radius), context + '/radius'); near(actual.borderWidth, parseFloat(expected.borderWidth), context + '/border-width');
  near(actual.opacity, Number(expected.opacity), context + '/opacity'); near(actual.strokeWidth, Number(expected.strokeWidth), context + '/stroke-width');
  assert.equal(actual.disabled, expected.disabled); assert.equal(actual.focusVisible, expected.focusVisible); assert.equal(actual.name, 'Close preview');
  near(actual.translateY, expected.translate === '0px 1px' ? 1 : 0, context + '/translate'); near(actual.scale, Number(expected.scale), context + '/scale');
  const shadow = [...expected.shadow.matchAll(/rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([\d.]+))?\)\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+([\d.]+)px\s+(-?[\d.]+)px/g)].filter(item => +item[7] > 0 && (item[4] === undefined || +item[4] > 0));
  assert(shadow.length <= 1);
  if (shadow.length) {
    assert(actual.shadow, context + '/shadow'); const wanted = shadow[0];
    for (const [field, index] of [['r', 1], ['g', 2], ['b', 3]]) assert.equal(actual.shadow[field], +wanted[index]);
    near(actual.shadow.y, +wanted[6], context + '/shadow-y'); near(actual.shadow.blur, +wanted[7], context + '/shadow-blur'); near(actual.shadow.spread, +wanted[8], context + '/shadow-spread');
    assert(Math.abs(actual.shadow.alpha - +wanted[4]) < .000001); assert.equal(actual.shadow.exteriorMask, true);
  } else assert.equal(actual.shadow, null, context + '/absent-shadow');
  const sourceBytes = await fs.readFile(path.join(sourceFolder, row.file));
  assert.equal(crypto.createHash('sha256').update(sourceBytes).digest('hex'), row.pngSha256);
  const wanted = readRgbaPng(sourceBytes), rendered = readRgbaPng(await fs.readFile(path.join(native, row.file)));
  assert.deepEqual([rendered.width, rendered.height], [wanted.width, wanted.height]);
  let differingPixels = 0, absoluteRgbaError = 0;
  for (let at = 0; at < wanted.rgba.length; at += 4) {
    let differs = false;
    for (let channel = 0; channel < 4; channel++) { const difference = Math.abs(wanted.rgba[at + channel] - rendered.rgba[at + channel]); differs ||= difference > 0; absoluteRgbaError += difference; }
    differingPixels += Number(differs);
  }
  raster.push({file: row.file, differingPixels, absoluteRgbaError});
}
assert.equal(report.interactions.length, 2);
for (const theme of ['Light', 'Dark']) {
  const interaction = report.interactions.find(row => row.theme === theme); assert(interaction);
  for (const field of ['passed', 'parameterIdentity', 'consumerContentRetained', 'disabledRejected', 'templateReplacement', 'remount', 'releasedShadowGraphs', 'liveTheme', 'immediatePressTransform']) assert.equal(interaction[field], true, theme + '/' + field);
  near(interaction.heldTransform.TranslateY, 1, theme + '/held-translate'); near(interaction.heldTransform.ScaleX, .97, theme + '/held-scale-x'); near(interaction.heldTransform.ScaleY, .97, theme + '/held-scale-y');
  assert.equal(interaction.heldTransform.clicks, 0);
  assert.equal(interaction.livePaint.scheme, theme === 'Light' ? 'Dark' : 'Light');
  const liveReference = reference.cases.find(row => row.id === interaction.livePaint.id && row.scheme === interaction.livePaint.scheme);
  assert(liveReference); paint(interaction.livePaint.background, liveReference.measurement.background, theme + '/live-theme-background');
  assert.equal(interaction.clicks, 3); assert.equal(interaction.Executions, 3);
}
const desktop = await read(path.join(native, 'dialog-close-desktop.json'));
const heldKeys = await read(path.join(native, 'dialog-close-held-keys.json')), heldPointer = await read(path.join(native, 'dialog-close-held-pointer.json'));
assert.equal(heldKeys.length, 6); assert.equal(heldPointer.length, 4); assert.equal(desktop.keyActions.length, 4);
for (const key of heldKeys) {
  assert.equal(key.downs, 1); assert.equal(key.ups, 1); assert(key.downOwned && key.upOwned);
  assert([9, 32].includes(key.key)); assert.equal(key.focusTransferred, key.key === 9);
}
for (const pointer of heldPointer) {
  assert(pointer.downDelivered && pointer.upDelivered);
  for (const pressed of [true, false]) assert.equal(pointer.receipts.filter(receipt => receipt.exact && receipt.pressed === pressed && receipt.owned && !receipt.IsGenerated).length, 1);
}
for (const key of desktop.keyActions) { assert.equal(key.downs, 1); assert.equal(key.ups, 1); assert.equal(key.targetRetained, true); }
for (const move of desktop.pointerMoves) assert(move.positioned && !move.deliveryFailed && (move.stationary || move.delivered));
for (const gate of desktop.pointerPresentations) assert(gate.passed && gate.stable >= 2 && gate.milliseconds < 2000);
const audit = {passed: errors.length === 0, frames: report.frames.length, maximumGeometryError, errors, realInputThemes: 2,
  heldKeyPairs: heldKeys.length, heldPointerPairs: heldPointer.length, activationKeyPairs: desktop.keyActions.length, pointerPresentationGates: desktop.pointerPresentations.length, raster,
  masks: 0, resampling: false, pixelTolerance: 0, sourceRasterParityVerified: raster.every(row => row.differingPixels === 0), normalNativeMotionVerified: false, nativeComponentAccepted: false};
await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n');
console.log(JSON.stringify({...audit, raster: undefined})); assert(audit.passed, 'Dialog Close Button reference differs.');
