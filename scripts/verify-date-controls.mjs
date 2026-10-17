import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';

const option = name => {
  const index = process.argv.indexOf(name);
  assert(index >= 0 && process.argv[index + 1], `Missing ${name}`);
  return path.resolve(process.argv[index + 1]);
};
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const reference = await read(new URL('../tests/Reference/DateControls.json', import.meta.url));
let frames = 0, days = 0, phases = 0, pointerPairs = 0, pointerGates = 0, heldKeys = 0;
for (const [width, folder] of [[320, option('--native-320')], [640, option('--native-640')]]) {
  const result = await read(path.join(folder, 'result.json'));
  assert.equal(result.passed, true); assert.equal(result.component, 'date-controls');
  assert.equal(result.environment.processArchitecture, 'X64');
  assert.equal(result.environment.displayScale, 1); assert.equal(result.environment.textScale, 1);
  const report = await read(path.join(folder, 'date-controls.json'));
  assert.equal(report.passed, true);
  const expected = reference.cases.filter(row => row.kind === 'calendar' && row.width === width);
  assert.equal(report.frames.length, expected.length); assert.equal(expected.length, 66);
  for (const source of expected) {
    const matches = report.frames.filter(row => row.id === source.id && row.state === source.state && row.scheme === source.scheme && row.width === width);
    assert.equal(matches.length, 1, source.file); const frame = matches[0];
    assert.equal(frame.ActualWidth, source.calendar.box.width); assert.equal(frame.ActualHeight, source.calendar.box.height);
    assert.equal(frame.days.length, source.calendar.days.length);
    for (const expectedDay of source.calendar.days) {
      const actual = frame.days.find(day => Number(day.day) === expectedDay.day); assert(actual);
      assert.equal(actual.enabled, !expectedDay.disabled); assert.equal(actual.selected, expectedDay.pressed); assert.equal(actual.name, expectedDay.label);
      for (const [native, browser] of [['X', 'x'], ['Y', 'y'], ['Width', 'width'], ['Height', 'height']])
        assert(Math.abs(actual.rectangle[native] - expectedDay.box[browser]) < .001, `${source.file}/${actual.day}/${browser}`);
      days++;
    }
    assert((await fs.stat(path.join(folder, source.file))).size > 0); frames++;
  }
  for (const theme of ['Light', 'Dark']) {
    const phase = name => {
      const matches = report.phases.filter(row => row.phase === `${theme}-${name}`);
      assert.equal(matches.length, 1); phases++; return matches[0];
    };
    const controlled = phase('controlled'); assert.equal(controlled.requested, '2026-10-19'); assert.equal(controlled.monthRetained, true); assert.equal(controlled.bindingRetained, true);
    const short = phase('four-rows'); assert.equal(short.height, 218); assert.equal(short.days, 28);
    const picker = phase('picker'); assert.equal(picker.selected, '2026-10-19'); assert.equal(picker.cleared, true); assert.equal(picker.focusReturned, true);
    const dismissal = phase('dismissal'); assert.equal(dismissal.escapeFocusRestored, true); assert.equal(dismissal.outsideFocusRetained, true);
    const remount = phase('rejected-and-remounted'); assert.equal(remount.rejectedValueRetained, true); assert.equal(remount.liveMonthRetained, true); assert.equal(remount.reopenedMonth, '2024-02');
    const lifecycle = phase('lifecycle'); for (const key of ['disableClosed', 'templateClosed', 'staleProviderRejected', 'unmountClosed']) assert.equal(lifecycle[key], true);
    const range = phase('range'); assert.equal(range.boundVerified, true); assert.equal(range.endpointRetained, true); assert.deepEqual(range.preset, {From:'2026-10-03', To:'2026-10-17'});
    for (const action of ['picker-select', 'picker-clear', 'range-from', 'rejected']) {
      const pointer = await read(path.join(folder, `date-controls-menu-pointer-${action}-${theme}.json`));
      assert.equal(pointer.passed, true); assert.equal(pointer.layer, 'winui-input-pointer-source');
      assert.equal(pointer.downs, 1); assert.equal(pointer.ups, 1); assert.deepEqual(pointer.order, ['down', 'up']);
      assert.equal(pointer.handlersDetached, true); assert.deepEqual(pointer.errors, []);
      assert.equal(pointer.receipts.length, 2); for (const receipt of pointer.receipts) {assert.equal(receipt.exactSource, true); assert.equal(receipt.exact, true);}
      pointerPairs++;
    }
  }
  assert.equal(report.phases.length, 14);
  const desktop = await read(path.join(folder, 'date-controls-desktop.json'));
  for (const move of desktop.pointerMoves) { assert.equal(move.positioned, true); assert(!move.deliveryFailed); assert(move.stationary || move.delivered); }
  for (const gate of desktop.pointerPresentations) { assert.equal(gate.passed, true); assert(gate.stable >= 2); assert(gate.milliseconds < 2000); pointerGates++; }
  const keys = await read(path.join(folder, 'date-controls-held-keys.json'));
  assert.equal(keys.length, expected.filter(row => row.state === 'focus').length + 2);
  assert.equal(keys.filter(row => row.key === 27).length, 2);
  for (const key of keys) { assert.equal(key.downs, 1); assert.equal(key.ups, 1); assert.equal(key.downOwned, true); assert.equal(key.upOwned, true); assert.equal(key.focusTransferred, true); heldKeys++; }
}
const audit = {passed:true, calendarFrames:frames, comparedDays:days, interactionPhases:phases, closingPointerPairs:pointerPairs, pointerGates, heldKeys,
  pickerAndRangeSourceGeometryVerified:false, sourceRasterParityVerified:false, nativeComponentAccepted:false};
await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n');
console.log(JSON.stringify(audit));
