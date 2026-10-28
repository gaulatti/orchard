import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';

export function verifyContextRetirement(s) {
 assert.equal(s.isOpen,false); assert.equal(s.remainingPopups,0);
 assert.equal(s.originalItemLoaded,false);assert.equal(s.opens,2);assert.equal(s.closes,2);
}
export function verifyContextOptions(n) {
 assert.equal(n.format,'orchard-context-menu-options-native-v1');
 assert.equal(n.passed,true); assert.equal(n.cases.length,38);
 assert(n.guards>=600); assert.equal(n.focus,0); assert.equal(n.activations,0);
 assert.equal(n.publicApisOnly,true); assert.equal(n.transientRootRequested,true);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','actualOutsidePointerRoutingVerified','actualFocusRoutingVerified','sourceDomRemountSideEffectsReplicated','nativeComponentAccepted']) assert.equal(n[key],false,key);
 const required={
  'default-and-input-plane':['defaultModalTrue','boundModeRetained','originalItemsRetained','backdropMatchesMode'],
  'live-bound-mode':['noSyntheticNativeLifecycle','originalPortalRetained','modalityBindingStayedLive','restoredBackdropVerified'],
  'cancel-dismiss-and-root-teardown':['canceledCloseRetainsSurface','nativeActionDismisses','markerAndPresentationRetired','modalityBindingRetainedAfterReopen']
 };
 for(const scheme of ['Light','Dark']) for(const placement of ['standalone','hosted','owned']) for(const modal of [false,true]) {
  for(const [name,flags] of Object.entries(required)) {
   const rows=n.cases.filter(c=>c.scheme===scheme&&c.placement===placement&&c.modal===modal&&c.name===name);assert.equal(rows.length,1);
   const c=rows[0];assert.equal(c.passed,true);for(const flag of flags)assert.equal(c[flag],true,flag);
   if(name==='default-and-input-plane')assert.equal(c.nativeOutsideHitStackVerified,!(placement!=='owned'&&modal));
   if(name==='cancel-dismiss-and-root-teardown'){assert.equal(c.opens,2);assert.equal(c.closes,2);assert.equal(c.invokes,1);verifyContextRetirement(c.retirement);}
  }
 }
 for(const scheme of ['Light','Dark']) {
  const rows=n.cases.filter(c=>c.scheme===scheme&&c.name==='nonmodal-owner-shield');assert.equal(rows.length,1);const c=rows[0];assert.equal(c.placement,'owned');assert.equal(c.modal,false);assert.equal(c.passed,true);
  for(const key of ['nonmodalStillOwned','inactiveTreeShielded','callerEnabledRetained','ownerResumeVerified'])assert.equal(c[key],true,key);
 }
 assert.equal(n.inputSamples.length,32);
 assert.equal(n.inputSamples.filter(s=>s.expectedModal).length,14);
 for(const s of n.inputSamples) {
  assert(['Light','Dark'].includes(s.scheme));assert(['standalone','hosted','owned'].includes(s.placement));assert.equal(typeof s.initialModal,'boolean');assert.equal(typeof s.expectedModal,'boolean');
  const {x,y}=s.outsidePoint,b=s.menuBounds;for(const v of [x,y,b.x,b.y,b.width,b.height])assert(Number.isFinite(v));assert(b.width>=160&&b.width<=900&&b.height>0&&b.height<=760);
  assert(!(x>=b.x&&x<=b.x+b.width&&y>=b.y&&y<=b.y+b.height),'Outside sample overlaps menu');assert.equal(s.menuContentHit,true);assert(Number.isInteger(s.internalWrapperHits)&&s.internalWrapperHits>=0);
  if(s.expectedModal){assert.equal(s.planeHit,true);assert(s.ownOutsideHits>=1);}else{assert.equal(s.planeHit,false);assert.equal(s.ownOutsideHits,0);assert.equal(s.outsideTargetInHitStack,true);}
 }
 for(const key of ['librarySha256','hostSha256'])assert.match(n[key],/^[0-9a-f]{64}$/);
 return {passed:true,cases:38,nativeInputSurfaceSamples:32,nonmodalSamples:18,modalSamples:14,guards:n.guards,publicRootOptionVerified:true,liveModalityBindingVerified:true,nativeLifecycleAndOwnershipVerified:true,nativeOutsideHitStackVerified:true,librarySha256:n.librarySha256,hostSha256:n.hostSha256,actualOutsidePointerRoutingVerified:false,actualFocusRoutingVerified:false,sourceDomRemountSideEffectsReplicated:false,nativeComponentAccepted:false};
}
async function main(){
 const option=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];},folder=option('--native');
 const n=JSON.parse(await fs.readFile(path.join(folder,'context-menu-options.json'))),audit=verifyContextOptions(n);
 for(const [file,sha]of [['Orchard.dll',n.librarySha256],[option('--host-assembly'),n.hostSha256]])assert.equal(crypto.createHash('sha256').update(await fs.readFile(path.join(folder,'app-identity',file))).digest('hex'),sha);
 assert.deepEqual(JSON.parse(await fs.readFile(path.join(folder,'process-exit.json'))),{exited:true,exitCode:0,timedOut:false});
 await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
