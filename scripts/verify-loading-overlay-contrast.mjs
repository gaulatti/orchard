import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output');
const read=async name=>JSON.parse((await fs.readFile(path.join(native,name),'utf8')).replace(/^\uFEFF/,''));
const png=async name=>readRgbaPng(await fs.readFile(path.join(native,name)));
const result=await read('result.json'),report=await read('loading-overlay-contrast.json');
assert.equal(result.passed,true);assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
assert.equal(typeof report.entryOnly,'boolean');assert.equal(result.component,report.entryOnly?'loading-overlay-contrast-entry':'loading-overlay-contrast');
const phases=report.entryOnly?['before-Light','before-Dark']:['before-Light','active-Light','restored-Light','before-Dark','active-Dark','restored-Dark'];
assert.deepEqual(report.reports.map(state=>state.id),phases);
const expected={Light:{Tint:['#FFFFFFFF',.72],Card:['#FFFFFFFF',1],Border:['#FFE6D5B8',.25],Text:['#FF2D2D2D',1],Spinner:['#FF2C5784',1]},Dark:{Tint:['#FF0D1821',.72],Card:['#FF182533',1],Border:['#FFFFFFFF',.1],Text:['#FFF0F4F8',1],Spinner:['#FF5BA3F5',1]}};
const rgb=hex=>[parseInt(hex.slice(3,5),16),parseInt(hex.slice(5,7),16),parseInt(hex.slice(7,9),16),255];
const ink=[];
for(const state of report.reports){
 const active=state.id.startsWith('active-');assert.equal(state.os.Enabled,active);assert.equal(state.highContrast,active);
 assert.equal(state.hasBlur,!active);assert.equal(state.hasShadow,!active);assert.equal(state.focusPreserved,true);assert.equal(state.rootTabStop,false);
 assert.equal(state.name,'Preparing local report');assert.equal(state.viewport.Width,640);assert.equal(state.viewport.Height,480);
 for(const[key,[color,opacity]]of Object.entries(expected[state.scheme])){
  const paint=state.palette[key];assert.equal(paint.Color,active?(key==='Tint'||key==='Card'?state.systemWindow:key==='Spinner'?state.systemHighlight:state.systemText):color);
  assert(Math.abs(paint.Opacity-(active?1:opacity))<.000001);
 }
 if(state.id.startsWith('before-')){
  const raster=await png(`contrast-ink-loading-label-${state.scheme}-before.png`),color=rgb(state.palette.Text.Color);let pixels=0;
  for(let at=0;at<raster.rgba.length;at+=4)if(color.every((value,index)=>raster.rgba[at+index]===value))pixels++;
  assert(pixels>0,'No real solid label ink was captured.');ink.push({scheme:state.scheme,pixels});
 }
 if(active){
  const details=await read(`contrast-ink-loading-label-${state.scheme}-details.json`);assert(details.Pixels.length>0);assert.deepEqual(details.unmatched,[]);assert.equal(details.expected,state.systemText);
  const image=await png(`contrast-active-${state.scheme}.png`),at=((image.height-20)*image.width+Math.floor(image.width/2))*4;
  assert.deepEqual(Array.from(image.rgba.subarray(at,at+4)),rgb(state.systemWindow),'Actual opaque backdrop differs from Windows Window.');
 }
}
for(const theme of['Light','Dark']){
 if(report.entryOnly){const image=await png(`catalog-${theme.toLowerCase()}.png`);assert.equal(image.width,640);assert.equal(image.height,480);}
 else{
  const before=report.reports.find(state=>state.id==='before-'+theme),restored=report.reports.find(state=>state.id==='restored-'+theme);assert.deepEqual(restored.palette,before.palette);assert.deepEqual(restored.os,before.os);
  for(const phase of['before','active','restored']){const image=await png(`contrast-${phase}-${theme}.png`);assert.equal(image.width,640);assert.equal(image.height,480);}
 }
}
if(!report.entryOnly){
 const input=await read('loading-overlay-contrast-desktop.json');assert.equal(input.pointerClicks.filter(click=>click.target==='PART_Portal'&&click.delivered&&click.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated)).length,2);
 assert.equal(input.keyActions.length,2);for(const key of input.keyActions){assert.equal(key.key,32);assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
 const stages=(await fs.readFile(path.join(native,'contrast-native-stages.jsonl'),'utf8')).trim().split(/\r?\n/).map(line=>JSON.parse(line).phase);
 assert.equal(stages.filter(stage=>stage==='enable-after').length,2);assert.equal(stages.filter(stage=>stage==='restore-after').length,2);
}
const audit={passed:true,entryOnly:report.entryOnly,actualContrastVerified:!report.entryOnly,componentAccepted:false,themes:2,labelInk:ink,ordinaryEffectsVerified:true,liveSystemColorsAndEffectSuppression:!report.entryOnly,windowsStateRestored:!report.entryOnly,narratorVerified:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
