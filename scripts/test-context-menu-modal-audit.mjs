import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {verifyContextMenuModal, verifyModalPixels, verifyNativeMenuDimensions, verifyNativeMenuReflow} from './verify-context-menu-modal.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => {
  const index = process.argv.indexOf(name);
  assert(index >= 0 && process.argv[index + 1]);
  return process.argv[index + 1];
};
const folder = option('--native');
const native = JSON.parse(await fs.readFile(path.join(folder, 'context-menu-modal.json')));
verifyContextMenuModal(native);

const changes = [
  report => { report.passed = false; },
  report => { report.cases.pop(); },
  report => { report.pngs.pop(); },
  report => { report.guards = 0; },
  report => { report.focus = 1; },
  report => { report.activations = 1; },
  report => { report.publicApisOnly = false; },
  report => { report.hardwareInputSent = true; },
  report => { report.nativeFocusVerified = true; },
  report => { report.nativeComponentAccepted = true; },
  report => { report.cases[0].originalObjectsRetained = false; },
  report => { report.cases[0].nativeItemAccessibility = false; },
  report => { report.cases[1].width = report.cases[0].width; },
  report => { report.cases[1].liveThemeUpdated = false; },
  report => { report.cases[1].enabledBindingRetained = false; },
  report => { report.cases.find(c => c.name === 'owner-shield-and-resume').inactiveMenuHidden = false; },
  report => { report.cases.find(c => c.name === 'owner-shield-and-resume').directInvocationRetained = false; },
  report => { report.cases.find(c => c.name === 'canceled-hide').canceledHideRetainsMenu = false; },
  report => { report.cases.find(c => c.name === 'owner-teardown-and-remount').canceledMenuRetired = false; },
  report => { report.cases.find(c => c.name === 'owner-teardown-and-remount').closes = 3; },
  report => { report.cases.find(c => c.name === 'caller-content-and-style').replacementContentMounted = false; },
  report => { report.cases.find(c => c.name === 'caller-content-and-style').livePresenterStyleApplied = false; },
  report => { report.pngs[0].file = '../wrong.png'; },
];
for (const change of changes) {
  const corrupt = structuredClone(native);
  change(corrupt);
  assert.throws(() => verifyContextMenuModal(corrupt));
}

let wrongThemePixelsRejected = 0;
for (const capture of native.pngs) {
  const png = readRgbaPng(await fs.readFile(path.join(folder, capture.file)));
  verifyModalPixels(capture, png);
  assert.throws(() => verifyModalPixels(capture, {...png, rgba: Buffer.alloc(png.rgba.length)}));
  wrongThemePixelsRejected++;
}
const old = JSON.parse(await fs.readFile(new URL('../tests/Reference/ContextMenuAdapterCounterexamples.json', import.meta.url)));
assert.equal(old.geometry.nativeReportPassed, false);
assert.equal(old.reflow.nativeReportPassed, false);
assert.throws(() => verifyNativeMenuDimensions(old.geometry.snapshot));
assert.throws(() => verifyNativeMenuReflow(old.reflow.initialWidth, old.reflow.dynamicWidth));

const result = {passed: true, corruptReportsRejected: changes.length, wrongThemePixelSamplesRejected: wrongThemePixelsRejected, actualDimensionCounterexamplesRejected: 1, actualReflowCounterexamplesRejected: 1};
await fs.writeFile(option('--output'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result));
