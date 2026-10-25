import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {readScreenshotPngCodec} from './reference/screenshot-png-codec.mjs';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const read=async p=>JSON.parse(await fs.readFile(p));
export async function auditShadowLifetime(folder,host){
 assert(['Orchard.Catalog','PackageConsumer'].includes(host));
 const r=await read(folder+'/lifetime.json'),exit=await read(folder+'/process-exit.json'),process=await read(folder+'/process.json');
 assert(['orchard-overlay-shadow-lifetime-native-v2','orchard-overlay-shadow-lifetime-native-v3'].includes(r.format));assert(['default','near','far','combined','near-rectangle','combined-rectangle','far-empty','combined-empty'].includes(r.mode));if(r.mode.endsWith('-empty'))assert.equal(r.format,'orchard-overlay-shadow-lifetime-native-v3');const emptyCaster=r.mode==='far-empty'||r.mode==='combined-empty';
 assert(r.passed&&!r.hostVisible&&!r.hardwareInputSent&&!r.osSettingsChanged&&!r.nativeComponentAccepted);
 assert.equal(r.focusEvents,0);assert.equal(r.activations,0);assert(r.guards>50);
 assert(exit.exited&&!exit.timedOut&&exit.exitCode===0&&!process.foregroundAuthorized&&!process.hardwareInputSent);assert.equal(process.mode,r.mode);
 assert.equal(hash(await fs.readFile(folder+'/app-identity/Orchard.dll')),r.librarySha256);
 assert.equal(hash(await fs.readFile(folder+'/app-identity/'+host+'.dll')),r.hostSha256);
 const codec=await readScreenshotPngCodec();assert.equal(r.pngExport,'pinned-edge-page-screenshot-injective-straight-v1');assert.equal(r.referenceCodecSha256,codec.sha256);
 let frames=0;
 async function frame(f){
  assert.equal(path.basename(f.file),f.file);assert.equal(path.basename(f.pngFile),f.pngFile);
  assert.equal(f.width,520);assert.equal(f.height,440);assert.equal(f.stableFrames,2);assert(f.attempts>=2&&f.attempts<=6);
  const raw=await fs.readFile(folder+'/'+f.file),pngBytes=await fs.readFile(folder+'/'+f.pngFile);assert.equal(hash(raw),f.sha256);assert.equal(hash(pngBytes),f.pngSha256);assert.equal(raw.length,520*440*4);
  const png=readRgbaPng(pngBytes);assert.equal(png.width,520);assert.equal(png.height,440);
  for(let at=0;at<raw.length;at+=4){assert.equal(png.rgba[at+3],raw[at+3]);for(const[n,p]of[[0,2],[1,1],[2,0]]){assert(raw[at+n]<=raw[at+3]);assert.equal(png.rgba[at+p],codec.table[raw[at+3]*256+raw[at+n]]);}}
  frames++;return raw;
 }
 function publicAlive(s){assert(s.loaded&&s.visual&&s.resources===21&&s.layers===2&&s.visibilityObservers>=3);}
 function publicSuspended(s){assert(!s.visual&&s.resources===0&&s.layers===0);}
 function primitiveAlive(s,kind){if(emptyCaster&&kind==='far')assert.equal(s.emptyCaster,true);if((r.mode==='near-rectangle'||r.mode==='combined-rectangle'||r.mode==='combined-empty')&&kind==='near'){assert(s.general);assert.equal(s.shared.kernelCount,1);assert.equal(s.shared.kernelLeaseCount,1);assert.equal(s.shared.kernelBytes,512);assert.deepEqual(s.shared.activeRadii,[{radius:0,owners:1}]);assert.deepEqual(s.shared.activeShapes,[{radius:0,width:0,height:0,actualRadius:0,owners:1}]);}assert(!s.disposed&&!s.queued&&s.graphResourceCount===(kind==='near'?7:13));assert(s.shared.registered&&!s.shared.disposed&&s.shared.ownerCount===1&&s.shared.deviceLostSubscribed&&!s.shared.sharedDeviceDisposed);}
 function primitiveSuspended(s,kind){assert(!s.disposed&&!s.queued&&s.graphResourceCount===0);assert(s.shared.registered&&!s.shared.disposed&&s.shared.ownerCount===1&&!s.shared.sharedDeviceDisposed);if(kind==='near')assert(s.shared.kernelCount===0&&s.shared.kernelLeaseCount===0&&s.shared.kernelBytes===0);else assert(s.planeCount===0&&s.shared.tableCount===1&&s.shared.tableBytes===2048);}
 function primitiveDead(s,kind){if((r.mode==='near-rectangle'||r.mode==='combined-rectangle'||r.mode==='combined-empty')&&kind==='near'){assert.equal(s.shared.kernelCount,0);assert.equal(s.shared.kernelLeaseCount,0);assert.equal(s.shared.kernelBytes,0);assert.deepEqual(s.shared.activeRadii,[]);assert.deepEqual(s.shared.activeShapes,[]);}assert(s.disposed&&!s.queued&&s.graphResourceCount===0&&s.shared.disposed&&!s.shared.registered&&s.shared.ownerCount===0&&!s.shared.deviceLostSubscribed&&!s.shared.sharedDeviceDisposed);}
 function group(s,alive,dead){assert(s.disposed===dead&&!s.queued&&s.graphResourceCount===(alive?22:0));assert.deepEqual(s.graph,{entryCount:alive?1:0,leases:alive?2:0,ownedClipCount:alive?2:0});}
 function ownerAlive(s){if(r.mode==='default'){assert.equal(s,null);return;}if(r.mode==='combined'||r.mode==='combined-rectangle'||r.mode==='combined-empty'){group(s,true,false);primitiveAlive(s.near,'near');primitiveAlive(s.far,'far');return;}primitiveAlive(s,r.mode==='near-rectangle'?'near':r.mode==='far-empty'?'far':r.mode);}
 function ownerSuspended(s){if(r.mode==='default'){assert.equal(s,null);return;}if(r.mode==='combined'||r.mode==='combined-rectangle'||r.mode==='combined-empty'){group(s,false,false);primitiveSuspended(s.near,'near');primitiveSuspended(s.far,'far');return;}primitiveSuspended(s,r.mode==='near-rectangle'?'near':r.mode==='far-empty'?'far':r.mode);}
 function ownerDead(s){if(r.mode==='default'){assert.equal(s,null);return;}if(r.mode==='combined'||r.mode==='combined-rectangle'||r.mode==='combined-empty'){group(s,false,true);primitiveDead(s.near,'near');primitiveDead(s.far,'far');return;}primitiveDead(s,r.mode==='near-rectangle'?'near':r.mode==='far-empty'?'far':r.mode);}
 const baseline=await frame(r.baseline);if(r.mode==='near-rectangle'){const reset=r.resourceReset;assert(reset&&reset.profileRegenerated&&reset.graphRetained&&reset.bindingRetained&&!reset.actualDeviceLossSimulated);ownerAlive(reset.before);ownerAlive(reset.after);assert.equal(reset.after.graphGenerations,reset.before.graphGenerations);assert.equal(reset.after.shared.generation,reset.before.shared.generation+1);assert(baseline.equals(await frame(reset.frame)));}else assert.equal(r.resourceReset,null);assert.deepEqual(r.bindings.map(b=>b.id),['binding-0','binding-1','binding-2']);let interior=0;const paintHashes=[];
 for(const b of r.bindings){assert(b.bindingRetained&&b.backgroundReferenceRetained&&b.opacityRetained);const on=await frame(b.on),off=await frame(b.off);let checked=0;for(let y=155;y<245;y++)for(let x=175;x<(emptyCaster?185:345);x++){const at=(y*520+x)*4;assert(on.subarray(at,at+4).equals(off.subarray(at,at+4)));checked++;}assert.equal(b.interiorPixels,checked);interior+=checked;paintHashes.push(hash(on));}
 assert.equal(new Set(paintHashes).size,3);assert.equal(hash(baseline),paintHashes[0]);
 const scenarios=['self-collapsed','parent-collapsed','root-collapsed','zero-width','zero-height','empty-template'];if(r.mode!=='default')scenarios.push('host-hidden');assert.deepEqual(r.suspensions.map(s=>s.scenario),scenarios);
 for(const s of r.suspensions){publicAlive(s.beforePublic);ownerAlive(s.beforeOwner);if(s.scenario==='host-hidden')publicAlive(s.hiddenPublic);else publicSuspended(s.hiddenPublic);ownerSuspended(s.hiddenOwner);publicAlive(s.resumedPublic);ownerAlive(s.resumedOwner);assert(s.bindingRetained&&s.visiblePixelsRestored&&!s.osSettingsChanged);assert(baseline.equals(await frame(s.frame)));}
 const p=r.reparent;publicSuspended(p.removedPublic);assert(!p.removedPublic.loaded&&p.removedPublic.visibilityObservers===0);ownerDead(p.removedOwner);publicSuspended(p.reparentHidden);publicAlive(p.resumedPublic);ownerAlive(p.resumedOwner);assert(p.oldParentNoLongerAffectsPixels&&p.bindingRetained);assert(baseline.equals(await frame(p.resumed)));assert(baseline.equals(await frame(p.formerParentChanged)));
 publicSuspended(r.final.component);assert(!r.final.component.loaded&&r.final.component.visibilityObservers===0);ownerDead(r.final.owner);assert.equal(r.final.nearRegistry,0);assert.equal(r.final.farRegistry,0);
 return {passed:true,mode:r.mode,librarySha256:r.librarySha256,hostSha256:r.hostSha256,boundPaintCases:3,rectangleProfileRegenerationVerified:r.mode==='near-rectangle',emptyFarCasterLifetimeVerified:emptyCaster,interiorPixelsChecked:interior,suspensionTransitions:scenarios.length,reparentAndFormerAncestorVerified:true,visibilityObserversRemoved:true,lastOwnerCacheReleased:true,framesVerified:frames,comparedRestoredPixels:(scenarios.length+2)*520*440,allowedChannelDifference:0,osSettingsChanged:false,hardwareInputSent:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const[folder,host,output]=process.argv.slice(2);assert(folder&&host&&output,'Usage: output-folder host-name audit-json');const r=await auditShadowLifetime(folder,host);await fs.writeFile(output,JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));}
