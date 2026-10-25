import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const phases = ['mounted','activation','cleared','retired-presentation','retired-metadata','retired-progress','restored-loading','bindings','restored-activation','retired-event-detached','caller-disabled','unloaded','remounted-activation','loading-retirement','default-resumed','default-resumed-activation'];
const variants = ['Primary','Secondary','Outline','Subtle','Ghost','Link','Destructive'];
export function verifyButtonLifecycle(n) {
  assert(['orchard-button-lifecycle-native-v1','orchard-button-lifecycle-native-v2'].includes(n.format));
  if(n.format==='orchard-button-lifecycle-native-v2') assert.equal(n.renderOwnerLifetimeControlled,true);
  assert.equal(n.passed,true); assert.equal(n.error,undefined); assert.deepEqual(n.failures,[]);
  assert.equal(n.nativeReadinessAsserted,true); assert.equal(n.nativeComponentAccepted,false);
  for (const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','settingsChanged']) assert.equal(n[key],false,key);
  for (const key of ['focus','activations']) assert.equal(n[key],0,key);
  assert(n.guards>=400); assert.equal(n.checks.length,28*phases.length);
  const expected = [], images = [];
  for (const theme of ['Light','Dark']) for (const variant of variants) for (const template of ['default','custom']) {
    const id=`${theme}/${variant}/${template}`;
    for (const phase of phases) expected.push(id+'/'+phase);
    for (const phase of ['initial','restored','default-resumed']) images.push(id.replaceAll('/','-')+'-'+phase+'.png');
    const get = phase => n.checks.find(c=>c.id===id+'/'+phase).observed;
    const mounted=get('mounted'); assert.equal(mounted.IsLoaded,true); assert.equal(mounted.actionLoaded,true); assert.equal(mounted.progressLoaded,true); assert.equal(mounted.readyFocusEligible,true);
    const cleared=get('cleared'); assert.equal(cleared.IsLoaded,true); assert.equal(cleared.actionLoaded,false); assert.equal(cleared.progressLoaded,false);
    const presentation=get('retired-presentation'); assert.equal(presentation.styleRetained,true); assert.equal(presentation.height,presentation.oldHeight); assert.equal(presentation.font,presentation.oldFont); assert.equal(presentation.padding,presentation.oldPadding);
    const metadata=get('retired-metadata'); assert.equal(metadata.name,metadata.oldName);
    const progress=get('retired-progress'); assert.equal(progress.active,progress.oldActive); assert.equal(progress.visibility,progress.oldVisibility);
    const loading=get('restored-loading'); for (const key of ['newAction','newProgress','IsLoaded','IsActive','loadingFocusAbsent']) assert.equal(loading[key],true); assert.equal(loading.IsEnabled,false); assert.equal(loading.name,'Updated caller name');
    const bindings=get('bindings'); for (const key of ['contentBindingRetained','loadingBindingRetained','enabledBindingRetained','IsLoading','IsEnabled']) assert.equal(bindings[key],true); assert.equal(bindings.content,'Updated fictional action');
    assert.deepEqual(get('activation'),{clicks:1,Executions:1}); assert.deepEqual(get('restored-activation'),{clicks:2,Executions:2,restoredFocusEligible:true});
    const observation=n.observations.find(o=>o.id===id+'/retired-native-invoke'); assert(observation); assert.equal(observation.clickDelta,0);
    const detached=get('retired-event-detached'); assert.equal(detached.clicks,detached.beforeClicks);
    assert.equal(get('caller-disabled').IsEnabled,false); assert.equal(get('caller-disabled').actionEnabled,false); assert.equal(get('caller-disabled').disabledFocusAbsent,true);
    assert.equal(get('unloaded').IsLoaded,false); assert.equal(get('unloaded').actionLoaded,false);
    assert.equal(get('remounted-activation').clicks,3); assert.equal(get('remounted-activation').Executions,3+observation.commandDelta);
    const retired=get('loading-retirement'); assert.equal(retired.actionLoaded,false); assert.equal(retired.progressLoaded,false); assert.equal(retired.IsLoading,true); assert.equal(retired.IsEnabled,true); assert.equal(retired.customProgress,template==='custom'); if(template==='default'){assert.equal(retired.ownedRotationPresent,true);assert.equal(retired.angle,0);}
    const resumed=get('default-resumed'); for (const key of ['newAction','IsLoaded','IsActive','loadingBindingRetained']) assert.equal(resumed[key],true); assert.equal(resumed.IsEnabled,false);
    assert.equal(get('default-resumed-activation').clicks,4); assert.equal(get('default-resumed-activation').Executions,4+observation.commandDelta);
  }
  assert.deepEqual(n.checks.map(c=>c.id).sort(),expected.sort()); for (const c of n.checks) assert.equal(c.passed,true,c.id);
  assert.equal(n.observations.length,28); assert.deepEqual(n.images.map(p=>p.file).sort(),images.sort());
  for (const p of n.images) { assert(p.width>0&&p.height>0); assert.match(p.sha256,/^[a-f0-9]{64}$/); }
  for (const key of ['librarySha256','hostSha256']) assert.match(n[key],/^[a-f0-9]{64}$/);
  return {passed:true,checks:n.checks.length,cases:28,images:n.images.length,focusEligibilityChecks:112,loadingAndDisabledFocusEligibilityVerified:true,templateRetirementVerified:true,callerBindingsRetained:true,exactlyOnceActivationVerified:true,librarySha256:n.librarySha256,hostSha256:n.hostSha256,...(n.format==='orchard-button-lifecycle-native-v2'?{renderOwnerLifetimeControlled:true}:{}),nativeComponentAccepted:false};
}
export async function auditButtonLifecycle(native,hostName) {
  const bytes=await fs.readFile(path.join(native,'button-lifecycle.json')),n=JSON.parse(bytes),audit=verifyButtonLifecycle(n);
  const exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json'))); assert.equal(exit.exited,true); assert.equal(exit.exitCode,0); assert.equal(exit.observationExpired,false);
  for (const [file,sha] of [['Orchard.dll',n.librarySha256],[hostName+'.dll',n.hostSha256]]) assert.equal(hash(await fs.readFile(path.join(native,'app-identity',file))),sha);
  for (const p of n.images) { const bytes=await fs.readFile(path.join(native,p.file)); assert.equal(hash(bytes),p.sha256); const png=readRgbaPng(bytes); assert.equal(png.width,p.width); assert.equal(png.height,p.height); }
  return {...audit,reportSha256:hash(bytes)};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  const [native,hostName,output]=process.argv.slice(2); const result=await auditButtonLifecycle(native,hostName); await fs.writeFile(output,JSON.stringify(result,null,2)+'\n'); console.log(JSON.stringify(result));
}
