import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {packageLibrary} from './verify-package-popover-modal-root.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function auditPackageTooltipModel(result) {
 assert.equal(result.format,'orchard-package-tooltip-modal-root-v1');assert.equal(result.passed,true);assert.equal(result.publicApisOnly,true);
 for(const k of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','nativeInputFocusVerified','nativeComponentAccepted'])assert.equal(result[k],false,k);
 assert.equal(result.focus,0);assert.equal(result.activations,0);assert(result.guards>=200);assert.equal(result.softwareNativeInvokes,2);
 const fields={
  'original-model-and-selector':{originalModel:true,originalSelector:true,visibleTemplate:true,opens:1},
  'newer-modal-pauses-tooltip':{requestRetained:true,pointerBlocked:true,peerExcluded:true,bindingRetained:true},
  'resume-live-template':{originalModel:true,liveValueRendered:true,peerPresent:true},
  'service-removal':{closedOnce:true,descriptionReleased:true,ownerReleased:true},
  'public-modal-owned-tooltip':{originalContent:true,explicitTheme:true,descriptionLinked:true},
  'owned-tooltip-suspension':{inactivePointerBlocked:true,inactivePeerExcluded:true,originalContentResumed:true},
  'original-owner-invocation':{nativeInvoke:true,confirmationRequests:1,parentRetained:true,tooltipClosed:true},
  'nested-popover-lifetime':{originalOwner:true,openedWithParent:true,closedWithOwner:true}
 };
 assert.equal(result.cases.length,16);assert.deepEqual(result.cases.map(c=>c.scheme+':'+c.name),['Light','Dark'].flatMap(s=>Object.keys(fields).map(n=>s+':'+n)));
 for(const c of result.cases)for(const [key,value]of Object.entries(fields[c.name]))assert.equal(c[key],value,c.scheme+':'+c.name+':'+key);
}
export function auditPackageTooltip(folder,packagePath) {
 const read=p=>JSON.parse(fs.readFileSync(path.join(folder,p))),result=read('package-tooltip-modal-root.json'),exit=read('process-exit.json'),bytes=fs.readFileSync(packagePath);
 assert(exit.exited&&!exit.timedOut&&exit.exitCode===0);auditPackageTooltipModel(result);
 assert.equal(result.librarySha256,hash(packageLibrary(bytes)));assert.equal(result.librarySha256,hash(fs.readFileSync(path.join(folder,'app-identity/Orchard.dll'))));assert.equal(result.hostSha256,hash(fs.readFileSync(path.join(folder,'app-identity/PackageConsumer.dll'))));
 return {passed:true,cases:16,softwareNativeInvokes:2,guards:result.guards,publicApisOnly:true,packageSha256:hash(bytes),librarySha256:result.librarySha256,hostSha256:result.hostSha256,nativeInputFocusVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1],n);return process.argv[i+1];};const result=auditPackageTooltip(arg('--native'),arg('--package'));fs.writeFileSync(arg('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));}
