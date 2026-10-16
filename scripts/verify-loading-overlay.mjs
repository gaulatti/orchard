import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const native=option('--native'),output=option('--output');
const reference=await read(new URL('../tests/Reference/LoadingOverlay.json',import.meta.url));
assert.equal(hash((await fs.readFile(new URL('./reference/capture-loading-overlay.cjs',import.meta.url),'utf8')).replace(/\r\n/g,'\n')),reference.scriptSha256);
const result=await read(path.join(native,'result.json')),report=await read(path.join(native,'loading-overlay-source.json'));
assert.equal(result.passed,true);assert.equal(result.component,'loading-overlay');
assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.highContrast,false);
assert.equal(report.cases.length,20);assert.deepEqual(report.failures,[]);
const expected=reference.cases.filter(test=>test.width===report.cases[0].width&&test.height===report.cases[0].height);
assert.equal(expected.length,20);
let maximumGeometryError=0,maximumBaselineError=0,baselines=0;
const near=(actual,source)=>{assert(Number.isFinite(actual));const delta=Math.abs(actual-source);maximumGeometryError=Math.max(maximumGeometryError,delta);assert(delta<=.05,`${actual} != ${source}`);};
const box=(actual,source)=>{for(const key of['x','y','width','height'])near(actual[key[0].toUpperCase()+key.slice(1)],source[key]);};
for(const [index,test]of expected.entries()){
 const actual=report.cases[index];assert.equal(actual.id,test.id);assert.equal(actual.scheme,test.scheme);assert.deepEqual(actual.errors,[]);
 assert.equal(actual.measurements.length,test.normal.overlays.length);
 for(const [overlayIndex,target]of test.normal.overlays.entries()){
  const overlay=actual.measurements[overlayIndex];box(overlay.portal,target.box);box(overlay.card,target.card.box);box(overlay.spinner,target.spinner.box);
  if(target.label){
   box(overlay.label.box,target.label.box);assert.equal(overlay.label.text,target.label.text.replace(/[ \t\r\n\f]+/g,' ').trim());
   if(target.label.firstBaseline!=null){near(overlay.label.baseline,target.label.firstBaseline);maximumBaselineError=Math.max(maximumBaselineError,Math.abs(overlay.label.baseline-target.label.firstBaseline));baselines++;}
  }else assert.equal(overlay.label,null);
 }
 const png=readRgbaPng(await fs.readFile(path.join(native,test.file)));assert.equal(png.width,test.width);assert.equal(png.height,test.height);
}
assert.equal(report.interactions.length,2);
for(const interaction of report.interactions){
 assert.equal(interaction.focusOnOpen,true);assert.equal(interaction.keyboardActivations,2);assert.equal(interaction.pointerBlocked,true);assert.equal(interaction.escapeDoesNotDismiss,true);
 assert.equal(interaction.neutralRootFocused,true);assert.equal(interaction.pointerClearedActivation,true);
 assert.equal(interaction.rootTabStopRestored,true);assert.equal(interaction.rootBindingRestored,true);
 assert.equal(interaction.focusAfterPointer,'CatalogRoot');assert.equal(interaction.focusAfterTab,'UnderlyingAction');assert.equal(interaction.focusAfterHide,'UnderlyingAction');
}
const input=await read(path.join(native,'loading-overlay-desktop.json'));
assert(input.pointerPresentations.length>=20);for(const gate of input.pointerPresentations)assert(gate.passed&&gate.milliseconds<2000);
assert.equal(input.keyActions.length,8);for(const key of input.keyActions)assert(key.downs===1&&key.ups===1&&key.targetRetained);
assert.equal(input.pointerClicks.length,2);
const delivered=new Set();
for(const move of input.pointerMoves){
 assert(move.positioned);assert.equal(move.actual.X,move.requested.x);assert.equal(move.actual.Y,move.requested.y);
 assert.equal(move.hit.root.handle,move.hit.own.handle);assert.equal(move.hit.foreground.handle,move.hit.own.handle);
 const point=`${move.requested.x},${move.requested.y}`;
 if(move.stationary)assert(delivered.has(point),'Stationary movement lacks an earlier real receipt.');
 else{assert(move.delivered&&move.deliveryMilliseconds<2000);assert(move.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));delivered.add(point);}
}
for(const click of input.pointerClicks)assert(click.delivered&&click.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));
const audit={format:'orchard-loading-overlay-audit-v1',passed:true,componentAccepted:false,cases:20,baselines,maximumGeometryError,maximumBaselineError,nativeClientCaptures:20,sourcePixelParityVerified:false,interactions:report.interactions,presentationGates:input.pointerPresentations.length};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
