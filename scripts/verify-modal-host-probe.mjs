import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
export async function auditModalHostProbe({folder,app,reference,report:provided,pngOverride=new Map()}){
 const report=provided??JSON.parse(await fs.readFile(path.join(folder,'modal-host-core.json')));
 assert.equal(report.format,'orchard-modal-host-core-probe-v1');assert.equal(report.passed,true);assert.deepEqual(report.failures,[]);
 assert.equal(report.scope,'hidden-internal-modal-layout-pointer-query-peer-and-binding-prototype');
 for(const key of ['activateCalled','showCalled','focusCalled','inputSent','popupOpened','actualFocusCancellationVerified','actualKeyboardVerified','nativeAlertDialogImplemented','nativeModalAcceptanceVerified','completeSourceRasterCompared'])assert.equal(report[key],false,key);
 assert.equal(report.activations,0);assert.equal(report.focus,0);assert(report.guards>=350);assert.equal(report.processArchitecture,'X64');
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,0);
 assert.equal(hash(await fs.readFile(path.join(app,'Orchard.dll'))),report.librarySha256);assert.equal(hash(await fs.readFile(path.join(app,'Orchard.Catalog.dll'))),report.hostSha256);
 const deps=JSON.parse(await fs.readFile(path.join(app,'Orchard.Catalog.deps.json')));assert(deps.libraries['Microsoft.WindowsAppSDK/2.5.1']);assert(deps.libraries['Microsoft.WindowsAppSDK.WinUI/2.3.9']);
 assert.equal(hash(await fs.readFile(reference)),report.referenceSha256);
 const scenarios=['binding-and-pointer-shield','branch-and-layer-order','simultaneous-ancestry','ancestor-first-teardown','reopen-before-detach','replacement-and-branch-release'];
 assert.equal(report.cases.length,12);let samples=0,frameValues=0,peerScopes=0;
 for(const scheme of ['Light','Dark'])for(const scenario of scenarios){
  const rows=report.cases.filter(row=>row.scheme===scheme&&row.scenario===scenario);assert.equal(rows.length,1);const row=rows[0];assert.equal(row.passed,true);assert(row.samples.length>=5);
  const byStage=new Map(row.samples.map(sample=>[sample.stage,sample]));assert.equal(byStage.size,row.samples.length);assert(byStage.has('background-only'));assert(byStage.has(scenario==='binding-and-pointer-shield'?'detached':'all-detached'));
  for(const sample of row.samples){
   samples++;assert.deepEqual(sample.host,{x:0,y:0,width:800,height:600});assert.equal(sample.bindingsRetained,true);assert.equal(sample.backgroundPointerBlocked,sample.active!==null);assert.equal(sample.backgroundFocusAllowed,sample.active===null);
   if(sample.active!==null)assert.equal(sample.backgroundHit,false);
   const activeLayers=sample.layers.filter(layer=>layer.active).sort((a,b)=>a.zIndex-b.zIndex);
   assert.equal(activeLayers.at(-1)?.name??null,sample.active);
   activeLayers.forEach((layer,index)=>assert.equal(layer.zIndex,50+index));
   for(const layer of sample.layers){
    assert.deepEqual(layer.frame,{x:0,y:0,width:800,height:600});frameValues+=4;assert.equal(layer.pointerAllowed,layer.name===sample.active);
    for(const branch of layer.branches){assert.equal(branch.zIndex,layer.zIndex+1);assert.equal(branch.pointerAllowed,layer.name===sample.active);assert.deepEqual(branch.frame,{x:260,y:240,width:170,height:60});frameValues+=4;}
   }
   assert(Array.isArray(sample.peerNames));
   if(sample.active!==null){peerScopes++;assert(!sample.peerNames.includes('Original action'));assert(!sample.peerNames.includes('Dynamic background'));assert(!sample.peerNames.includes('Replacement background'));}
  }
  if(scenario==='binding-and-pointer-shield'){
   for(const stage of ['blocked','caller-updated-while-blocked','dynamic-background-blocked','deactivated-presence-retained'])assert(byStage.has(stage));
   const blocked=byStage.get('blocked');assert.equal(blocked.originalEnabled,true);assert.equal(blocked.originalHitTestVisible,true);assert.deepEqual(blocked.peerNames,[]);
   for(const stage of ['caller-updated-while-blocked','dynamic-background-blocked','deactivated-presence-retained']){const s=byStage.get(stage);assert.equal(s.originalEnabled,false);assert.equal(s.originalHitTestVisible,false);assert.equal(s.originalText,'Updated during modal');}
   assert(byStage.get('deactivated-presence-retained').peerNames.includes('Dynamic background'));assert.equal(byStage.get('detached').backgroundHit,true);
  }else{
   const nested=byStage.get('nested-active-child');assert(nested);assert.equal(nested.active,'AlertSurface');assert.equal(nested.layers.length,3);assert(nested.peerNames.includes('AlertSurface action'));assert(!nested.peerNames.includes('Parent branch'));
   if(scenario==='ancestor-first-teardown'){const s=byStage.get('inactive-parent-closed');assert(s);assert.equal(s.active,'AlertSurface');assert.equal(s.layers.find(l=>l.name==='ModalSurface').zIndex,50);assert.equal(s.layers.find(l=>l.name==='AlertSurface').zIndex,51);}
   else if(scenario==='reopen-before-detach'){assert.equal(byStage.get('child-closing').active,'ModalSurface');assert.equal(byStage.get('child-reopened').active,'AlertSurface');}
   else {const s=byStage.get('parent-and-branch-active');assert(s);assert.equal(s.active,'ModalSurface');assert(s.peerNames.includes('Parent branch'));assert(!s.peerNames.includes('AlertSurface action'));}
   if(scenario==='replacement-and-branch-release'){assert.equal(byStage.get('replacement-background-blocked').backgroundPointerBlocked,true);const s=byStage.get('branch-detached');assert(s);assert(s.layers.every(l=>l.branches.length===0));assert(!s.peerNames.includes('Parent branch'));}
   assert.equal(byStage.get('all-inactive-paint-retained').backgroundHit,true);assert.equal(byStage.get('all-detached').layers.length,0);
  }
 }
 const pngs=new Map();assert.equal(report.captures.length,14);
 for(const row of report.captures){assert.equal(path.basename(row.file),row.file);assert(!pngs.has(row.file));assert.equal(row.width,800);assert.equal(row.height,600);assert.equal(row.scope,'complete-native-client-diagnostic');assert.equal(row.sourceRasterCompared,false);
  const bytes=pngOverride.get(row.file)??await fs.readFile(path.join(folder,row.file));assert.equal(hash(bytes),row.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,800);assert.equal(png.height,600);assert(png.rgba.some(v=>v!==0));pngs.set(row.file,png);
 }
 let unchangedPixels=0;for(const scheme of ['Light','Dark']){const before=pngs.get(scheme+'-unblocked.png'),blocked=pngs.get(scheme+'-blocked.png');assert(before&&blocked);assert.deepEqual(blocked.rgba,before.rgba,'Pointer shield changed original native paint.');unchangedPixels+=800*600;for(const scenario of scenarios.slice(1))assert(pngs.has(scheme+'-'+scenario+'-nested.png'));}
 return{passed:true,scope:report.scope,librarySha256:report.librarySha256,hostSha256:report.hostSha256,cases:12,samples,frameValues,peerScopes,sourceLayerDepths:3,encodedPngs:14,unchangedBackgroundPixels:unchangedPixels,guards:report.guards,callerBindingsRetained:true,actualNativeHitQueriesVerified:true,actualNativeFocusVerified:false,physicalInputVerified:false,nativeAlertDialogImplemented:false,nativeModalAcceptanceVerified:false,sourceRasterParityVerified:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
 const result=await auditModalHostProbe({folder:option('--native'),app:option('--app'),reference:option('--reference')});await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
}
