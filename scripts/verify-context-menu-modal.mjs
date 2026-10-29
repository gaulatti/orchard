import fs from 'node:fs/promises';import path from 'node:path';import crypto from 'node:crypto';import assert from 'node:assert/strict';import {fileURLToPath}from 'node:url';import {readRgbaPng}from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function verifyNativeMenuDimensions(d){assert(Math.abs(d.portalWidth-d.nativeWidth)<.001&&Math.abs(d.portalHeight-d.nativeHeight)<.001,'Projected menu does not use the native layout box');}
export function verifyNativeMenuReflow(initial,dynamic){assert(dynamic>initial+10,'Live content did not grow the native menu');}
export function verifyContextMenuModal(n){
 assert.equal(n.format,'orchard-context-menu-modal-native-v2');assert.equal(n.passed,true);assert.equal(n.cases.length,12);assert.equal(n.pngs.length,4);assert(n.guards>=200);assert.equal(n.focus,0);assert.equal(n.activations,0);assert.equal(n.publicApisOnly,true);
 for(const k of ['hardwareInputSent','physicalInputVerified','nativeFocusVerified','nativeComponentAccepted'])assert.equal(n[k],false,k);
 const required={
  'native-originals':['originalObjectsRetained','nativeItemAccessibility'],
  'live-content-theme-and-binding':['contentWidthGrew','enabledBindingRetained','liveThemeUpdated','originalPresenterRetained'],
  'caller-content-and-style':['replacementContentMounted','originalContentRestored','livePresenterStyleApplied','styleRestorationReflows'],
  'owner-shield-and-resume':['inactiveMenuHidden','callerStateRetained','directInvocationRetained','originalMenuResumed','nativeDismissalVerified'],
  'canceled-hide':['canceledHideRetainsMenu','subsequentHideClosesMenu'],
  'owner-teardown-and-remount':['canceledMenuRetired','originalActionsSurviveRemount']
 };
 for(const scheme of ['Light','Dark']){
  for(const [name,flags] of Object.entries(required)){
   const rows=n.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(rows.length,1);
   const c=rows[0];assert.equal(c.passed,true);for(const key of flags)assert.equal(c[key],true,key);
   if(name==='owner-teardown-and-remount')for(const key of ['opens','closes','clicks','commands'])assert.equal(c[key],4);
  }
  const initial=n.cases.find(c=>c.scheme===scheme&&c.name==='native-originals');
  const dynamic=n.cases.find(c=>c.scheme===scheme&&c.name==='live-content-theme-and-binding');
  assert(initial.width>=160&&initial.width<400);assert.equal(initial.height,122);
  verifyNativeMenuReflow(initial.width,dynamic.width);assert(dynamic.width<=900);assert.equal(dynamic.height,122);
 }
 assert.deepEqual(n.pngs.map(p=>p.file).sort(),['Light','Dark'].flatMap(s=>['initial','dynamic'].map(v=>`context-modal-${s}-${v}.png`)).sort());for(const p of n.pngs){assert.match(p.sha256,/^[0-9a-f]{64}$/);assert(p.width>=160&&p.width<=900);assert.equal(p.height,122);}for(const k of ['librarySha256','hostSha256'])assert.match(n[k],/^[0-9a-f]{64}$/);
 return{passed:true,cases:12,captures:4,guards:n.guards,nativeOwnedPresenterExercised:true,automaticOwnerRetirementVerified:true,nativeItemAccessibilityVerified:true,liveContentThemeAndBindingVerified:true,librarySha256:n.librarySha256,hostSha256:n.hostSha256,physicalInputVerified:false,nativeFocusVerified:false,nativeComponentAccepted:false};
}
export function verifyModalPixels(p,png){const dark=p.file.includes('-Dark-initial')||p.file.includes('-Light-dynamic'),expected=dark?[24,37,51,255]:[255,255,255,255],at=(115*png.width+7)*4;assert.deepEqual([...png.rgba.subarray(at,at+4)],expected,'Hosted menu paint does not match its initial or live theme');}
async function main(){const option=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];},folder=option('--native'),n=JSON.parse(await fs.readFile(path.join(folder,'context-menu-modal.json'))),audit=verifyContextMenuModal(n);for(const[file,sha]of [['Orchard.dll',n.librarySha256],[option('--host-assembly'),n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',file))),sha);let pixels=0;for(const p of n.pngs){const bytes=await fs.readFile(path.join(folder,p.file));assert.equal(hash(bytes),p.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,p.width);assert.equal(png.height,p.height);verifyModalPixels(p,png);pixels+=png.width*png.height;}assert.deepEqual(JSON.parse(await fs.readFile(path.join(folder,'process-exit.json'))),{exited:true,exitCode:0,timedOut:false});audit.pixelsValidated=pixels;audit.initialAndLiveThemePixelProbes=4;await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
