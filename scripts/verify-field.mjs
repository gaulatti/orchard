import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {cssRgba} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]); };
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const native = option('--native'), source = option('--source');
const reference = await read(new URL('../tests/Reference/Field.json', import.meta.url));
const result = await read(path.join(native, 'result.json')), report = await read(path.join(native, 'field.json'));
assert(result.passed && report.passed && result.component === 'field');
assert.equal(result.environment.displayScale, 1); assert.equal(result.environment.textScale, 1);
const key = row => row.id + '/' + row.scheme;
assert.equal(reference.cases.length, 44); assert.deepEqual(report.frames.map(key).sort(), reference.cases.map(key).sort());
let maximumGeometryError = 0; const errors = [], raster = [], markerLineBoxes = [];
const normalize = value => value.replace(/\s+/gu, ' ').trim();
function near(actual, expected, context) {
  assert(Number.isFinite(actual) && Number.isFinite(expected), context);
  const error = Math.abs(actual - expected); maximumGeometryError = Math.max(maximumGeometryError, error);
  if (error >= .001) errors.push({context, actual, expected, error});
}
function paint(actual, css, context) {
  const [r, g, b, alpha] = cssRgba(css); assert(actual, context);
  if (actual.r !== r || actual.g !== g || actual.b !== b || Math.abs(actual.a / 255 * actual.opacity - alpha) >= .000001) errors.push({context, actual, expected: css});
}
const relations = uia => uia.describedBy.map(item => item.id.endsWith('-description') ? 'description' : item.id.endsWith('-error') ? 'error' : item.id);
function peer(uia, context) {
  assert(Number.isInteger(uia.clientProcess) && Number.isInteger(uia.process) && uia.clientProcess !== uia.process, context + '/external-client');
  assert(Array.isArray(uia.runtimeId) && uia.runtimeId.length > 0); assert.equal(typeof uia.valid, 'boolean');
}
for (const row of reference.cases) {
  const actual = report.frames.find(frame => key(frame) === key(row)).measurement, expected = row.measurement, context = key(row);
  for (const subject of ['root', 'header', 'label', 'control', 'description', 'error', 'right']) {
    // A scalar zero is an anonymous browser text node, with no element box.
    // Its header baseline/height and native visible text are still checked.
    if (subject === 'right' && row.spec.action === 0) { assert.equal(actual.right.text, '0'); continue; }
    assert.equal(actual[subject] === null, expected[subject] === null, context + '/' + subject + '/presence');
    if (!expected[subject]) continue;
    for (const field of ['x', 'y', 'width', 'height']) near(actual[subject].box[field], expected[subject].box[field], context + '/' + subject + '/' + field);
  }
  for (const subject of ['label', 'description', 'error']) {
    if (!expected[subject]) continue;
    assert.equal(normalize(actual[subject].text), normalize(expected[subject].text), context + '/' + subject + '/text');
    for (const field of ['fontSize', 'fontWeight', 'lineHeight']) near(actual[subject][field], parseFloat(expected[subject][field]), context + '/' + subject + '/' + field);
    paint(actual[subject].foreground, expected[subject].foreground, context + '/' + subject + '/paint');
    assert(actual[subject].fontFamily.includes(subject === 'description' ? 'LibreFranklin-Regular.ttf' : subject === 'error' ? 'EncodeSans-Regular.ttf' : 'EncodeSans-Medium.ttf'));
  }
  if (actual.right) {
    const wanted = expected.right ?? expected.root;
    for (const field of ['fontSize', 'fontWeight', 'lineHeight']) near(actual.right[field], parseFloat(wanted[field]), context + '/right/' + field);
    paint(actual.right.foreground, wanted.foreground, context + '/right/paint');
    assert(actual.right.fontFamily.includes(row.spec.action === 'button' ? 'EncodeSans-Medium.ttf' : row.spec.action === 0 ? 'EncodeSans-Regular.ttf' : 'LibreFranklin-Regular.ttf'));
  }
  assert.equal(actual.required === null, expected.required === null, context + '/required-mark');
  if (expected.required) {
    // WinUI reports the TextBlock line box; browser span bounds report its
    // font box. Preserve both raw measurements; compare the font box when
    // captured rather than equating these different vertical quantities.
    markerLineBoxes.push({context, nativeLineBox: actual.required.box, sourceFontBox: expected.required.box});
    assert(actual.required.fontBox, context + '/required-font-box');
    for (const field of ['x', 'y', 'width', 'height']) near(actual.required.fontBox[field], expected.required.box[field], context + '/required-font-box/' + field);
    for (const field of ['fontSize', 'fontWeight', 'lineHeight']) near(actual.required[field], parseFloat(expected.required[field]), context + '/required/' + field);
    paint(actual.required.foreground, expected.required.foreground, context + '/required/paint');
  }
  paint(actual.inputBackground, expected.inputBackground, context + '/input-background'); paint(actual.inputBorder, expected.inputBorder, context + '/input-border');
  assert.equal(actual.value, expected.value); assert.equal(actual.valid, expected.invalid !== 'true');
  assert.equal(actual.nativeRequired, expected.nativeRequired); assert.equal(actual.disabled, expected.disabled);
  assert.deepEqual(actual.describedBy, expected.describedBy);
  if (Object.hasOwn(row.spec, 'controlId')) assert.equal(actual.controlId, row.spec.controlId);
  else assert.match(actual.controlId, /^orchard-field-[0-9a-f]{32}-control$/);
  const uia = actual.uia; peer(uia, context); assert.equal(uia.id, actual.controlId); assert.equal(uia.valid, actual.valid);
  assert.equal(uia.required, actual.nativeRequired); assert.equal(uia.enabled, !actual.disabled);
  const labelName = normalize(expected.label.text).replace(row.spec.required ? /\*$/ : /$^/, '');
  assert.equal(normalize(uia.name), labelName); assert.equal(normalize(uia.labeledBy.name), labelName);
  assert.deepEqual(relations(uia), expected.describedBy);
  assert.deepEqual(uia.describedBy.map(item => normalize(item.name)), expected.describedBy.map(id => id === 'external-help' ? 'Consumer description' : normalize(expected[id].text)));
  if (expected.right) assert.equal(actual.right.text, expected.right.text);
  const bytes = await fs.readFile(path.join(source, row.file)); assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'), row.pngSha256);
  const wanted = readRgbaPng(bytes), rendered = readRgbaPng(await fs.readFile(path.join(native, row.file)));
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
  const live = report.interactions.find(row => row.theme === theme); assert(live);
  for (const field of ['passed', 'identityRetained', 'labelFocused', 'templateReplacement', 'remount', 'childReplacement', 'restoredMetadata', 'boundMetadataRestored', 'ownedTextScaleRestored']) assert.equal(live[field], true, theme + '/' + field);
  assert.equal(live.actions, 1); assert.notEqual(live.retainedText, 'Consumer text'); assert(live.retainedText.includes('Consumer text'));
  const phases = ['initial', 'added', 'removedError', 'removedMessages'];
  for (const phase of phases) {
    const uia = live[phase]; peer(uia, theme + '/' + phase); assert.equal(uia.id, live.initialId); assert.deepEqual(uia.runtimeId, live.initial.runtimeId);
    assert.equal(uia.required, false); assert.equal(uia.enabled, true); assert.equal(uia.valid, phase !== 'added'); assert.equal(uia.name, 'Coverage title');
    const expected = phase === 'added' ? ['external-help', 'description', 'error'] : phase === 'removedError' ? ['external-help', 'description'] : ['external-help'];
    assert.deepEqual(relations(uia), expected);
    assert.deepEqual(uia.describedBy.map(item => item.name), expected.map(id => id === 'external-help' ? 'Consumer description' : id === 'description' ? 'Live description.' : 'Live error.'));
  }
}
const audit = {passed: errors.length === 0, frames: report.frames.length, maximumGeometryError, errors, realInputThemes: 2, externalUiaSnapshots: report.frames.length + report.interactions.length * 4,
  markerLineBoxes, raster, masks: 0, resampling: false, pixelTolerance: 0, sourceRasterParityVerified: raster.every(row => row.differingPixels === 0),
  actualScalingVerified: false, nativeArm64RuntimeVerified: false, nativeComponentAccepted: false};
await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n');
console.log(JSON.stringify({...audit, raster: undefined, markerLineBoxes: undefined})); assert(audit.passed, 'Field source geometry or paint differs.');
