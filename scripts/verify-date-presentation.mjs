import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {assertNativePaint} from './reference/css-paint.mjs';

const option = name => {const index = process.argv.indexOf(name); assert(index >= 0 && process.argv[index + 1], name); return path.resolve(process.argv[index + 1]);};
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const source = await read(new URL('../tests/Reference/DateControls.json', import.meta.url));
let frames = 0, boxes = 0, days = 0, paints = 0, pointerGates = 0, keyboardPairs = 0;
const errors = [];
const checkPaint = action => {try {action();} catch (error) {errors.push(error.message);}};
const near = (actual, expected, context) => assert(Math.abs(actual - expected) < .001, `${context}: ${actual} != ${expected}`);
function box(actual, expected, context) {
  assert(actual, context);
  for (const [native, browser] of [['X','x'],['Y','y'],['Width','width'],['Height','height']]) near(actual[native], expected[browser], `${context}/${browser}`);
  boxes++;
}
function paint(actual, expected, context) {
  checkPaint(() => assertNativePaint(actual.background, expected.backgroundColor, context + '/background'));
  checkPaint(() => assertNativePaint(actual.border, expected.borderColor, context + '/border'));
  near(actual.radius, parseFloat(expected.borderRadius), context + '/radius');
  near(actual.borderWidth, parseFloat(expected.borderWidth), context + '/border-width');
  paints++;
}
for (const [width, folder] of [[320, option('--native-320')], [640, option('--native-640')]]) {
  const result = await read(path.join(folder, 'result.json')); assert.equal(result.passed, true); assert.equal(result.component, 'date-presentation');
  assert.equal(result.environment.processArchitecture, 'X64'); assert.equal(result.environment.displayScale, 1); assert.equal(result.environment.textScale, 1);
  const report = await read(path.join(folder, 'date-presentation.json')); assert.equal(report.passed, true); assert.deepEqual(report.errors, []);
  const cases = source.cases.filter(row => row.kind !== 'calendar' && row.width === width); assert.equal(cases.length, 28); assert.equal(report.frames.length, cases.length);
  for (const expected of cases) {
    const matches = report.frames.filter(row => row.id === expected.id && row.kind === expected.kind && row.state === expected.state && row.scheme === expected.scheme && row.width === width);
    assert.equal(matches.length, 1, expected.file); const frame = matches[0]; assert.equal(frame.errors, 0);
    box(frame.root, expected.root.box, expected.file + '/root'); near(frame.scrollX, expected.viewport.scrollX, expected.file + '/scrollX');
    assert.equal(frame.triggers.length, expected.triggers.length); assert.equal(frame.buttons.length, expected.buttons.length);
    for (const [index, trigger] of frame.triggers.entries()) {
      const wanted = expected.triggers[index], computed = source.paints[wanted.paintRef], text = trigger.texts[0];
      box(trigger.box, wanted.box, expected.file + '/trigger-' + index); assert.equal(trigger.IsEnabled, !wanted.disabled); assert.equal(trigger.texts.length, 1);
      assert.equal(text.Text, wanted.text); assert.match(text.Source, /EncodeSans-Regular\.ttf#Encode Sans$/);
      near(text.FontSize, parseFloat(computed.fontSize), 'font-size'); near(text.LineHeight, parseFloat(computed.lineHeight), 'line-height'); assert.equal(text.weight, Number(computed.fontWeight));
      paint(trigger.chrome, computed, expected.file); checkPaint(() => assertNativePaint(text.foreground, computed.color, expected.file + '/text')); near(trigger.chrome.opacity, Number(computed.opacity), 'opacity');
    }
    for (const [index, button] of frame.buttons.entries()) box(button.box, expected.buttons[index].box, expected.file + '/button-' + index);
    if (expected.rangeCard) {box(frame.rangeCard, expected.rangeCard.box, expected.file + '/card'); paint(frame.rangePaint, source.paints[expected.rangeCard.paintRef], expected.file + '/card');}
    if (expected.popup) {
      box(frame.popup, expected.popup.box, expected.file + '/popup'); paint(frame.popupPaint, source.paints[expected.popup.paintRef], expected.file + '/popup');
      box(frame.calendar.box, expected.calendar.box, expected.file + '/calendar'); assert.equal(frame.calendar.month, expected.calendar.month); assert.equal(frame.calendar.days.length, expected.calendar.days.length);
      for (const wanted of expected.calendar.days) {
        const actual = frame.calendar.days.find(day => day.day === wanted.day); assert(actual); box(actual.box, wanted.box, expected.file + '/day-' + wanted.day);
        assert.equal(actual.enabled, !wanted.disabled); assert.equal(actual.selected, wanted.pressed); assert.equal(actual.name, wanted.label); days++;
      }
    } else {assert.equal(frame.popup, null); assert.equal(frame.calendar, null);}
    if (expected.clearAction) {box(frame.clearAction.box, expected.clearAction.box, expected.file + '/clear'); box(frame.clearAction.parent, expected.clearAction.parent, expected.file + '/clear-parent'); near(frame.clearAction.bottomGap, expected.clearAction.bottomGap, 'clear descent');}
    assert((await fs.stat(path.join(folder, expected.file))).size > 0); frames++;
  }
  const desktop = await read(path.join(folder, 'date-presentation-desktop.json'));
  for (const move of desktop.pointerMoves) {assert.equal(move.positioned, true); assert(!move.deliveryFailed); assert(move.stationary || move.delivered);}
  for (const gate of desktop.pointerPresentations) {assert.equal(gate.passed, true); assert(gate.stable >= 2); assert(gate.milliseconds < 2000); pointerGates++;}
  const keys = await read(path.join(folder, 'date-presentation-held-keys.json')); assert.equal(keys.length, cases.filter(row => row.state === 'open').length);
  for (const key of keys) {assert.equal(key.key, 27); assert.equal(key.downs, 1); assert.equal(key.ups, 1); assert.equal(key.downOwned, true); assert.equal(key.upOwned, true); assert.equal(key.focusTransferred, true); keyboardPairs++;}
}
const report = {passed:errors.length === 0, sourceFrames:frames, comparedBoxes:boxes, comparedPopupDays:days, basicPaintChecks:paints, pointerGates, keyboardPairs, errors,
  sourceShadowVerified:false, sourceRasterParityVerified:false, normalMotionVerified:false, nativeComponentAccepted:false};
await fs.writeFile(option('--output'), JSON.stringify(report, null, 2) + '\n'); console.log(JSON.stringify(report)); assert.deepEqual(errors, []);
