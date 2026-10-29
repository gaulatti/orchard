import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {verifyContextOptions, verifyContextRetirement} from './verify-context-menu-options.mjs';

const option = name => {
  const index = process.argv.indexOf(name);
  assert(index >= 0 && process.argv[index + 1]);
  return process.argv[index + 1];
};
const native = JSON.parse(await fs.readFile(path.join(option('--native'), 'context-menu-options.json')));
verifyContextOptions(native);

const changes = [
  report => { report.passed = false; },
  report => { report.cases.pop(); },
  report => { report.guards = 0; },
  report => { report.focus = 1; },
  report => { report.activations = 1; },
  report => { report.publicApisOnly = false; },
  report => { report.hardwareInputSent = true; },
  report => { report.actualOutsidePointerRoutingVerified = true; },
  report => { report.actualFocusRoutingVerified = true; },
  report => { report.sourceDomRemountSideEffectsReplicated = true; },
  report => { report.nativeComponentAccepted = true; },
  report => { report.cases[0].defaultModalTrue = false; },
  report => { report.cases[0].boundModeRetained = false; },
  report => { report.cases[0].originalItemsRetained = false; },
  report => { report.cases[0].outsideInputMatchesMode = false; },
  report => { report.cases[0].nativeItemAccessibilityVerified = false; },
  report => { report.cases[1].noSyntheticNativeLifecycle = false; },
  report => { report.cases[1].originalNativePresenterRetained = false; },
  report => { report.cases[1].modalityBindingStayedLive = false; },
  report => { report.cases[2].canceledCloseRetainsSurface = false; },
  report => { report.cases[2].nativePresenterRetired = false; },
  report => { report.cases[2].closes = 1; },
  report => { report.cases.find(c => c.name === 'nonmodal-owner-shield').inactiveTreeShielded = false; },
  report => { report.cases.find(c => c.name === 'nonmodal-owner-shield').callerEnabledRetained = false; },
  report => { report.librarySha256 = 'stale'; },
];
for (const change of changes) {
  const corrupt = structuredClone(native);
  change(corrupt);
  assert.throws(() => verifyContextOptions(corrupt));
}

const counterexample = JSON.parse(await fs.readFile(new URL('../tests/Reference/ContextMenuOptionsCounterexample.json', import.meta.url)));
assert.equal(counterexample.nativeReportPassed, false);
assert.throws(() => verifyContextRetirement(counterexample.snapshot));

const result = {passed: true, corruptReportsRejected: changes.length, actualRetirementCounterexamplesRejected: 1, rejectedLibrarySha256: counterexample.librarySha256};
await fs.writeFile(option('--output'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result));
