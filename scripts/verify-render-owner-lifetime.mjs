import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function verifyRenderOwnerLifetime(n) {
  assert.equal(n.format,'orchard-render-owner-lifetime-native-v1'); assert.equal(n.passed,true); assert.equal(n.error,undefined);
  assert.equal(n.retainedFramesCompleted,7); assert.equal(n.forcedCollections,7); assert.equal(n.sharedReadinessHelperVerified,true); assert.equal(n.forcedGcIsDiagnosticOnly,true);
  assert.equal(n.guards,15); assert.equal(n.focus,0); assert.equal(n.activations,0);
  for(const key of ['showCalled','activateCalled','focusCalled','hardwareInputSent','osSettingsChanged','nativeComponentAccepted'])assert.equal(n[key],false,key);
  assert.equal(n.winUiSha256,'aad12524765e6fb63f0ae26a45a9ba3104f24fde66413d8a3036fbed74a1e990'); assert.equal(n.framework,'10.0.12');
  assert.equal(n.observations.length,3); assert.deepEqual(n.observations.map(o=>o.attempt),[0,1,2]);
  for(const o of n.observations){assert.equal(o.ownerAlive,false);assert.equal(o.asyncStatus,'Started');assert.equal(o.managedTaskCompleted,false);assert.equal(o.subsequentStrongFramesCompleted,2);}
  for(const key of ['librarySha256','hostSha256'])assert.match(n[key],/^[a-f0-9]{64}$/);
  return {passed:true,retainedFramesCompleted:7,forcedCollections:7,actualUnretainedOrphans:3,sharedReadinessHelperVerified:true,winUiSha256:n.winUiSha256,librarySha256:n.librarySha256,hostSha256:n.hostSha256,nativeComponentAccepted:false};
}
export async function auditRenderOwnerLifetime(folder,host){
  const bytes=await fs.readFile(path.join(folder,'render-owner-lifetime.json')),n=JSON.parse(bytes),audit=verifyRenderOwnerLifetime(n);
  const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.observationExpired,false);
  for(const[file,sha]of[['Orchard.dll',n.librarySha256],[host+'.dll',n.hostSha256],['Microsoft.UI.Xaml.dll',n.winUiSha256]])assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',file))),sha);
  return {...audit,reportSha256:hash(bytes)};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
  const[folder,host,output]=process.argv.slice(2);const result=await auditRenderOwnerLifetime(folder,host);
  const native=JSON.parse(await fs.readFile(path.join(folder,'render-owner-lifetime.json')));
  const mutations=[n=>n.passed=false,n=>n.retainedFramesCompleted=6,n=>n.forcedCollections=0,n=>n.sharedReadinessHelperVerified=false,n=>n.forcedGcIsDiagnosticOnly=false,n=>n.guards=0,n=>n.focus=1,n=>n.activations=1,n=>n.hardwareInputSent=true,n=>n.osSettingsChanged=true,n=>n.nativeComponentAccepted=true,n=>n.observations.pop(),n=>n.observations[0].ownerAlive=true,n=>n.observations[0].asyncStatus='Completed',n=>n.observations[0].managedTaskCompleted=true,n=>n.observations[0].subsequentStrongFramesCompleted=0,n=>n.observations[1].attempt=0,n=>n.winUiSha256='invalid'];
  for(const mutate of mutations){const n=structuredClone(native);mutate(n);assert.throws(()=>verifyRenderOwnerLifetime(n));}
  const final={...result,corruptReportsRejected:mutations.length};await fs.writeFile(output,JSON.stringify(final,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(final));
}
