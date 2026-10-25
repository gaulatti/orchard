import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {verifyButtonLifecycle} from './verify-button-lifecycle.mjs';

const [native, before, output] = process.argv.slice(2);
const original = JSON.parse(await fs.readFile(path.join(native,'button-lifecycle.json')));
verifyButtonLifecycle(original);
const at = (n, phase) => n.checks.find(c=>c.id==='Light/Primary/default/'+phase).observed;
const mutations = [
  ['ready-action-not-focusable',n=>at(n,'mounted').readyFocusEligible=false],
  ['loading-action-focusable',n=>at(n,'restored-loading').loadingFocusAbsent=false],
  ['ready-focus-not-restored',n=>at(n,'restored-activation').restoredFocusEligible=false],
  ['disabled-action-focusable',n=>at(n,'caller-disabled').disabledFocusAbsent=false],
  ['false-success',n=>n.passed=false],['failure-erasure',n=>n.failures.push('retirement')],
  ['lost-readiness',n=>n.nativeReadinessAsserted=false],['invented-acceptance',n=>n.nativeComponentAccepted=true],
  ['visible-activation',n=>n.activateCalled=true],['show-window',n=>n.showWindowCalled=true],['focus-request',n=>n.focusCalled=true],
  ['hardware-input',n=>n.hardwareInputSent=true],['settings-change',n=>n.settingsChanged=true],['received-focus',n=>n.focus=1],['received-activation',n=>n.activations=1],
  ['missing-guards',n=>n.guards=0],['missing-phase',n=>n.checks.pop()],['duplicate-phase',n=>n.checks[1]=n.checks[0]],['failed-check',n=>n.checks[0].passed=false],
  ['unready-wrapper',n=>at(n,'mounted').IsLoaded=false],['unready-action',n=>at(n,'mounted').actionLoaded=false],['unready-progress',n=>at(n,'mounted').progressLoaded=false],
  ['old-action-loaded',n=>at(n,'cleared').actionLoaded=true],['old-progress-loaded',n=>at(n,'cleared').progressLoaded=true],
  ['retired-style-mutated',n=>at(n,'retired-presentation').styleRetained=false],['retired-height-mutated',n=>at(n,'retired-presentation').height+=1],
  ['retired-font-mutated',n=>at(n,'retired-presentation').font+=1],['retired-padding-mutated',n=>at(n,'retired-presentation').padding='invalid'],
  ['retired-name-mutated',n=>at(n,'retired-metadata').name+=' changed'],['retired-progress-active',n=>at(n,'retired-progress').active=!at(n,'retired-progress').oldActive],
  ['retired-progress-visible',n=>at(n,'retired-progress').visibility='invalid'],['reused-action',n=>at(n,'restored-loading').newAction=false],
  ['restored-loading-enabled',n=>at(n,'restored-loading').IsEnabled=true],['restored-loading-inactive',n=>at(n,'restored-loading').IsActive=false],
  ['wrong-restored-name',n=>at(n,'restored-loading').name='wrong'],['duplicate-click',n=>at(n,'restored-activation').clicks++],
  ['duplicate-command',n=>at(n,'restored-activation').Executions++],['retired-event',n=>n.observations[0].clickDelta=1],
  ['lost-disable',n=>at(n,'caller-disabled').actionEnabled=true],['unload-not-realized',n=>at(n,'unloaded').IsLoaded=true],
  ['duplicate-remount-command',n=>at(n,'remounted-activation').Executions++],['missing-native-observation',n=>n.observations.pop()],
  ['missing-image',n=>n.images.pop()],['escaping-image',n=>n.images[0].file='../outside.png'],['invalid-image-hash',n=>n.images[0].sha256='invalid'],
  ['invalid-library-hash',n=>n.librarySha256='invalid'],['invalid-host-hash',n=>n.hostSha256='invalid'],
  ['lost-content-binding',n=>at(n,'bindings').contentBindingRetained=false],['lost-loading-binding',n=>at(n,'bindings').loadingBindingRetained=false],['lost-enabled-binding',n=>at(n,'bindings').enabledBindingRetained=false],
  ['retired-loading-action-loaded',n=>at(n,'loading-retirement').actionLoaded=true],['spinner-still-rotating',n=>at(n,'loading-retirement').angle=20],['missing-owned-spinner',n=>at(n,'loading-retirement').ownedRotationPresent=false],
  ['default-not-resumed',n=>at(n,'default-resumed').newAction=false],['resumed-loading-active-input',n=>at(n,'default-resumed').IsEnabled=true],['resumed-duplicate-activation',n=>at(n,'default-resumed-activation').clicks++]
];
if(original.format==='orchard-button-lifecycle-native-v2') mutations.push(['lost-render-owner-retention',n=>n.renderOwnerLifetimeControlled=false]);
for (const [name,mutate] of mutations) { const n=structuredClone(original); mutate(n); assert.throws(()=>verifyButtonLifecycle(n),undefined,name); }
const error = JSON.parse(await fs.readFile(path.join(before,'unhandled-button-lifecycle-error.json')));
const stage = JSON.parse(await fs.readFile(path.join(before,'stage.json')));
const exit = JSON.parse(await fs.readFile(path.join(before,'process-exit.json')));
assert.equal(error.passed,false); assert.match(error.error,/BleeckerButton requires a native Button named PART_Action/);
assert.equal(stage.stage,'after-clear-before-layout'); assert.equal(stage.checks.length,2); assert(stage.checks.every(c=>c.passed)); assert.equal(exit.exitCode,1);
const result={passed:true,corruptReportsRejected:mutations.length,actualNullTemplateFailureRetained:true,priorMountAndActivationPassed:true,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(result,null,2)+'\n'); console.log(JSON.stringify(result));
