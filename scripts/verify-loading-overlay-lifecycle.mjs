import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output');
const read=async name=>JSON.parse((await fs.readFile(path.join(native,name),'utf8')).replace(/^\uFEFF/,''));
const result=await read('result.json'),report=await read('loading-overlay-lifecycle.json'),input=await read('loading-overlay-lifecycle-desktop.json');
assert.equal(result.passed,true);assert.equal(result.component,'loading-overlay-lifecycle');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
const ids=['page-root','live-theme-Light','live-theme-Dark','live-theme-Light','consumer-brushes','resize-320','resize-1024','resize-640','immediate-reparent','template-remove','template-restore','unload','remount','stack'];
assert.deepEqual(report.reports.map(test=>test.id),ids);
let portalMeasurements=0;
for(const [index,test]of report.reports.entries()){
 assert.equal(test.originalFocus,true,test.id);assert.equal(test.rootTabStop,false,test.id);
 assert.equal(test.portals.length,['template-remove','unload','stack'].includes(test.id)?0:1,test.id);
 for(const portal of test.portals){
  for(const [actual,expected]of[[portal.x,0],[portal.y,0],[portal.width,test.viewport.Width],[portal.height,test.viewport.Height]])assert(Number.isFinite(actual)&&Math.abs(actual-expected)<=.05);
  if(index<4){const dark=test.id==='live-theme-Dark';assert.equal(portal.cardColor,dark?'#FF182533':'#FFFFFFFF');assert.equal(portal.tintColor,dark?'#FF0D1821':'#FFFFFFFF');assert(Math.abs(portal.tintOpacity-.72)<.001);}
  else{assert.equal(portal.cardColor,'#FFF0FFFF');assert.equal(portal.tintColor,'#FFADD8E6');assert(Math.abs(portal.tintOpacity-.35)<.001);}
  portalMeasurements++;
 }
}
assert.equal(portalMeasurements,11);
const beforePointer=report.focusEvents.find(event=>event.kind==='before-pointer'),afterTab=report.focusEvents.find(event=>event.kind==='after-tab');
assert(beforePointer&&afterTab);assert.equal(beforePointer.IsTabStop,false);assert.equal(afterTab.IsTabStop,false);assert.equal(afterTab.focused,'Button');assert.equal(afterTab.effectiveValue,'False');
const captures=[];
for(const scheme of['light','dark']){
 const bytes=await fs.readFile(path.join(native,`catalog-${scheme}.png`)),image=readRgbaPng(bytes);
 assert.equal(image.width,640);assert.equal(image.height,480);
 captures.push({scheme,sha256:crypto.createHash('sha256').update(bytes).digest('hex')});
}
assert.notEqual(captures[0].sha256,captures[1].sha256,'Live theme captures are unchanged.');
const resizes=report.reports.filter(test=>test.id.startsWith('resize-'));
assert.deepEqual(resizes.map(test=>[test.viewport.Width,test.viewport.Height]),[[320,240],[1024,480],[640,480]]);
const click=input.pointerClicks.filter(click=>click.target==='PART_Portal');assert.equal(click.length,1);assert(click[0].delivered&&click[0].receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));
for(const gate of input.pointerPresentations)assert(gate.passed&&gate.milliseconds<2000);
const audit={passed:true,componentAccepted:false,phases:14,portalMeasurements,originalFocusPreserved:true,rootTabStopRestored:true,originalContentRestored:true,nativePointerBarrier:true,livePaletteAndConsumerBrushes:true,realResizes:resizes.map(test=>test.viewport),captures,sourcePixelParityVerified:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
