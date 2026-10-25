import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const near=(actual,expected,tolerance=.001)=>assert(Number.isFinite(actual)&&Math.abs(actual-expected)<=tolerance,`${actual} differs from ${expected}`);
export function verifyContextMenuRoot(n,reference){
 assert.equal(n.format,'orchard-context-menu-root-native-v3');assert.equal(n.passed,true);
 assert.equal(n.cases.length,2);assert(n.guards>=200);assert.equal(n.focus,0);assert.equal(n.activations,0);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','contextGestureVerified','physicalInputVerified','hostedModalAdapterExercised','submenuExpansionAttempted','nativeComponentAccepted','sourceRasterParityVerified','sourcePlacementVerified'])assert.equal(n[key],false,key);
 assert.equal(n.transientRootFlyoutRequested,true);assert.equal(n.selectionCancellationVerified,true);
 for(const scheme of ['Light','Dark']){
  const rows=n.cases.filter(c=>c.scheme===scheme);assert.equal(rows.length,1);const c=rows[0];
  for(const key of ['passed','originalNativeObjectsRetained','contextGeometryVerified','nativeCommandAndParameterRetained','nativeDismissalVerified','nativeClosingCancellationVerified','inheritedHideAfterCancellationVerified'])assert.equal(c[key],true,key);
  assert.deepEqual(c.nativeEventOrder,['click','command','closed','click','command','closed']);
  for(const key of ['opens','closes','clicks','commands'])assert.equal(c[key],2,key);
  assert.equal(c.rootMinimum,160);assert.equal(c.rowMinimum,36);assert.equal(c.font,13);assert.equal(c.lineHeight,19.5);near(c.disabledOpacity,.4);
 }
 const source=reference.cases.filter(c=>c.spec.kind==='context');assert.equal(source.length,10);assert.equal(n.layouts.length,10);assert.equal(n.pngs.length,10);let maximumWidthDifference=0,geometryValues=0;
 for(const c of source){
  const rows=n.layouts.filter(l=>l.scheme===c.scheme&&l.id===c.id);assert.equal(rows.length,1);const l=rows[0],m=c.measurement.menus[0];assert.equal(l.passed,true);assert.equal(l.placementVerified,false);assert.equal(l.minimum,160);assert.equal(l.radius,7);assert.deepEqual(l.padding,[6,6,6,6]);
  // DirectWrite and Chromium advance widths are recorded separately. The
  // source structural recipe is checked within one DIP; this is not raster parity.
  near(l.width,m.box.width,1);maximumWidthDifference=Math.max(maximumWidthDifference,Math.abs(l.width-m.box.width));near(l.height,m.box.height);assert.equal(l.background,c.scheme==='Light'?'#ffffffff':'#ff182533');assert.equal(l.border,c.scheme==='Light'?'#ffe6d5b8':'#ffffffff');near(l.borderOpacity,c.scheme==='Light'?.35:.15);assert.equal(l.rows.length,m.items.length);geometryValues+=4;
  for(let i=0;i<l.rows.length;i++){
   const r=l.rows[i],s=m.items[i];assert.equal(r.text,s.text);assert.equal(r.name,s.text);assert.equal(r.enabled,!s.disabled);assert.equal(r.font,13);assert.equal(r.lineHeight,19.5);assert(r.fontFamily.includes('EncodeSans-Regular.ttf'));assert.equal(r.radius,5);assert.deepEqual(r.padding,s.paint.padding);near(r.opacity,s.paint.opacity);assert.equal(r.foreground,c.scheme==='Light'?'#ff2d2d2d':'#fff0f4f8');near(r.box.x,s.box.x-m.box.x);near(r.box.y,s.box.y-m.box.y);near(r.box.height,s.box.height);near(r.box.width,l.width-14);geometryValues+=4;
  }
 }
 for(const key of ['sourceReferenceSha256','librarySha256','hostSha256'])assert.match(n[key],/^[0-9a-f]{64}$/);
 assert.deepEqual(n.pngs.map(p=>p.file).sort(),n.layouts.map(c=>`context-root-${c.scheme}-${c.id}.png`).sort());for(const p of n.pngs){assert.match(p.file,/^context-root-(Light|Dark)-context-(basic|inset|edge|unicode|empty)\.png$/);assert.match(p.sha256,/^[0-9a-f]{64}$/);assert(p.width>=160&&p.width<=162);assert([14,50,122].includes(p.height));}
 return {passed:true,nativeCommandCases:2,sourceRecipeLayouts:10,geometryValues,maximumWidthDifference,widthToleranceDip:1,captures:10,guards:n.guards,librarySha256:n.librarySha256,hostSha256:n.hostSha256,sourceReferenceSha256:n.sourceReferenceSha256,hostedModalAdapterExercised:false,contextGestureVerified:false,sourcePlacementVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
export function verifyContextMenuPixels(c,png){
 const expected=c.scheme==='Light'?[255,255,255]:[24,37,51];const x=7,y=png.height-7;assert(x<png.width&&y>=0&&y<png.height);const pixel=[...png.rgba.subarray((y*png.width+x)*4,(y*png.width+x)*4+4)];assert.deepEqual(pixel,[...expected,255],'Native Context Menu background is absent or differs from its theme');return 1;
}
async function main(){
 const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const folder=option('--native'),sourceBytes=await fs.readFile(path.join(folder,'source-reference.json')),n=JSON.parse(await fs.readFile(path.join(folder,'context-menu-root.json'))),audit=verifyContextMenuRoot(n,JSON.parse(sourceBytes));assert.equal(hash(sourceBytes),n.sourceReferenceSha256);
 for(const [file,sha]of [['Orchard.dll',n.librarySha256],[option('--host-assembly'),n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',file))),sha);
 let pixels=0,backgroundProbes=0;for(const p of n.pngs){const bytes=await fs.readFile(path.join(folder,p.file));assert.equal(hash(bytes),p.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,p.width);assert.equal(png.height,p.height);backgroundProbes+=verifyContextMenuPixels(n.layouts.find(c=>p.file===`context-root-${c.scheme}-${c.id}.png`),png);pixels+=png.width*png.height;}
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.deepEqual(exit,{exited:true,exitCode:0,timedOut:false});Object.assign(audit,{rawPixelsValidated:pixels,backgroundPixelProbes:backgroundProbes});await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
