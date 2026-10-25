import fs from 'node:fs/promises';import path from 'node:path';import assert from 'node:assert/strict';import crypto from'node:crypto';import{pathToFileURL}from'node:url';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export async function auditAlertActions(folder,provided){
 const r=provided??JSON.parse(await fs.readFile(path.join(folder,'alert-dialog-actions.json'))),exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(r.format,'orchard-alert-dialog-actions-v1');assert.equal(r.passed,true);assert.deepEqual(r.failures,[]);assert.equal(r.architecture,'X64');assert.equal(r.focus,0);assert.equal(r.activations,0);assert(r.guards>=150);assert.equal(r.softwareNativeInvokes,8);assert.equal(r.cases.length,22);for(const key of ['activateCalled','showCalled','focusCalled','hardwareInputSent','nativeKeyboardPointerVerified'])assert.equal(r[key],false,key);assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,0);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.dll'))),r.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.Catalog.dll'))),r.hostSha256);
 const expected={
  'confirm-native-invoke-requests-once':{events:1,executions:1,originalParameter:true,eventBeforeCommand:true,openBindingRetained:true},
  'cancel-native-invoke-requests-once':{events:1,executions:1,originalParameter:true,openBindingRetained:true},
  'command-state-controls-native-action':{disabledOnRejection:true,rejectedRequestIgnored:true,restoredActionEnabled:true,restoredInvokeOnce:true,openBindingRetained:true},
  'loading-rejects-both-actions-and-window-close':{bothDisabled:true,manualLoaderNotBusy:true,cancelIgnored:true,confirmIgnored:true,windowCloseIgnored:true,openBindingRetained:true},
  'window-pattern-close-remains-controlled':{cancelOnce:true,commandOnce:true,openBindingRetained:true},
  'live-label-description-and-variant':{titleUpdated:true,emptyDescriptionRelationshipRemoved:true,labelsUpdated:true,variantUpdated:true,openBindingRetained:true},
  'close-releases-input-before-removing-paint':{ownershipReleasedImmediately:true,paintRetainedAt60ms:true,removedAfter150ms:true,closingRequestsIgnored:true,callerBindingRetained:true},
  'rapid-reopen-reuses-presence-layer':{originalLayerRetained:true,immediateOwnershipRestored:true,settledOpen:true,openBindingRetained:true},
  'command-replacement-and-shared-subscription':{oldCommandsUnsubscribed:true,sharedCommandSubscriptions:1,replacementInvokedOnce:true,openBindingRetained:true},
  'template-replacement-remounts-controlled-value':{originalOpenBindingRetained:true,retiredLayerReleased:true,replacementLayerMounted:true},
  'unload-releases-commands-and-reload-reopens':{commandUnsubscribedOnUnload:true,layerReleasedOnUnload:true,callerOpenValueRetained:true,reloadMounted:true,reloadSubscriptions:1}
 };
 for(const scheme of ['Light','Dark'])for(const[name,evidence]of Object.entries(expected)){const rows=r.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(rows.length,1,scheme+'/'+name);assert.equal(rows[0].passed,true);assert.deepEqual(rows[0].evidence,evidence,scheme+'/'+name);}
 return{passed:true,cases:22,softwareNativeInvokes:8,guards:r.guards,librarySha256:r.librarySha256,hostSha256:r.hostSha256,controlledBindingAndCommandsVerified:true,presenceAndLifecycleVerified:true,hardwareInputSent:false,nativeKeyboardPointerVerified:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};const result=await auditAlertActions(option('--native'));await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));}
