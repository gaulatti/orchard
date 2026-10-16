import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output');
const read=async name=>JSON.parse((await fs.readFile(path.join(native,name),'utf8')).replace(/^\uFEFF/,''));
const result=await read('result.json'),report=await read('loading-overlay-windows.json');
assert.equal(result.passed,true);assert.equal(result.component,'loading-overlay-windows');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);assert.equal(report.otherClosed,true);
assert(Number.isInteger(report.primaryWindow)&&report.primaryWindow>0);assert(Number.isInteger(report.secondaryWindow)&&report.secondaryWindow>0);assert.notEqual(report.primaryWindow,report.secondaryWindow);
const ids=['primary-open','detach-primary','attach-secondary','old-primary-mutated','return-primary-immediate','old-secondary-mutated','secondary-identical-layout','primary-identical-layout','secondary-immediate','detach-secondary','primary-remount','old-window-closed','hidden'];
assert.deepEqual(report.reports.map(state=>state.id),ids);
const primaryCounts=[1,0,0,0,1,1,0,1,0,0,1,1,0],secondaryCounts=[null,0,1,1,0,0,1,0,1,0,0,null,null];
let visible=0;
for(const[index,state]of report.reports.entries()){
 const secondary=[2,3,6,8,9].includes(index),dark=[2,3,8,9].includes(index),hidden=[1,9,12].includes(index);
 assert.equal(state.owner,secondary?'secondary':'primary');assert.equal(state.theme,dark?'Dark':'Light');assert.equal(state.IsTabStop,false);assert.equal(state.otherClosed,index>=11);
 assert.equal(state.primaryPortals,primaryCounts[index]);assert.equal(state.secondaryPortals,secondaryCounts[index]);
 assert.equal(state.viewport.Width,secondary&&index!==6?320:640);assert.equal(state.viewport.Height,secondary&&index!==6?240:480);
 if(hidden){assert.equal(state.portal,null);continue;}
 visible++;assert.equal(state.focused,secondary?'SecondaryAction':'PrimaryAction');const portal=state.portal;
 assert.equal(portal.window,secondary?report.secondaryWindow:report.primaryWindow);assert.equal(portal.controlRootMatchesOwner,true);
 assert.equal(portal.theme,state.theme);assert.equal(portal.cardColor,dark?'#FF182533':'#FFFFFFFF');assert.equal(portal.name,'Shared operation');
 for(const[actual,expected]of[[portal.x,0],[portal.y,0],[portal.width,state.viewport.Width],[portal.height,state.viewport.Height]])assert(Number.isFinite(actual)&&Math.abs(actual-expected)<=.05);
}
assert.equal(visible,10);
for(const[owner,count]of[['primary',6],['secondary',4]]){
 const input=await read(`loading-overlay-windows-${owner}-desktop.json`);assert.equal(input.window,report[owner+'Window']);
 const clicks=input.pointerClicks.filter(click=>click.target==='PART_Portal');assert.equal(clicks.length,count);
 for(const click of clicks)assert(click.delivered&&click.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));
 assert.equal(input.keyActions.length,count);
 for(const key of input.keyActions){assert.equal(key.key,32);assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);assert.equal(key.target,owner==='primary'?'PrimaryAction':'SecondaryAction');}
 for(const gate of input.pointerPresentations)assert(gate.passed&&gate.stable>=2&&gate.milliseconds<2000);
}
const captures=[];
for(const[scheme,width,height]of[['light',640,480],['dark',320,240]]){
 const bytes=await fs.readFile(path.join(native,`catalog-${scheme}.png`)),png=readRgbaPng(bytes);assert.equal(png.width,width);assert.equal(png.height,height);
 captures.push({scheme,width,height,sha256:crypto.createHash('sha256').update(bytes).digest('hex')});
}
const audit={passed:true,componentAccepted:false,windows:2,phases:13,visiblePhases:10,physicalPointerBarriers:10,ownedSpaceActions:10,distinctRootOwnership:true,sameLayoutTransfers:true,oldRootCleanup:true,sourceWindowClosure:true,destinationThemeFocusAndName:true,captures,sourcePixelParityVerified:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
