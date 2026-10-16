import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output');
const read=async name=>JSON.parse((await fs.readFile(path.join(native,name),'utf8')).replace(/^\uFEFF/,''));
const result=await read('result.json'),report=await read('loading-overlay-template.json'),input=await read('loading-overlay-template-desktop.json');
assert.equal(result.passed,true);assert.equal(result.component,'loading-overlay-template');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
const ids=['default-resources','custom-light','custom-theme-Dark','custom-theme-Light','mutated-resources','live-properties','local-precedence','clear-local','replaced-dictionary','default-restored'];
assert.deepEqual(report.reports.map(state=>state.id),ids);
const colors=['#FFF5F5DC','#FFF5F5DC','#FF483D8B','#FFF5F5DC','#FFF0FFFF','#FFF0FFFF','#FFFFE4E1','#FF483D8B','#FF191970','#FF191970'];
for(const[index,state]of report.reports.entries()){
 assert.equal(state.colors,true);assert.equal(state.focusPreserved,true);assert.equal(state.customVisual,index>0&&index<9);assert.equal(state.cardColor,colors[index]);
 assert.equal(state.theme,[2,6,7,8,9].includes(index)?'Dark':'Light');
 assert(Math.abs(state.tintOpacity-(index>=8?.55:index===4||index===5?.3:.4))<.000001);
 for(const[actual,expected]of[[state.at.X,0],[state.at.Y,0],[state.ActualWidth,state.viewport.Width],[state.ActualHeight,state.viewport.Height]])assert(Number.isFinite(actual)&&Math.abs(actual-expected)<=.05);
 assert.equal(state.name,index<5?'Caller operation':'Updated caller operation');
 assert.equal(state.Text,index<5?'Caller template 日本語':'Updated caller label العربية');
 assert.equal(state.FontSize,index<5?14:18);assert.equal(state.CharacterSpacing,index<5?0:70);assert.equal(state.direction,index<5?'LeftToRight':'RightToLeft');
 assert.deepEqual(state.Padding,index<5?{Left:28,Top:24,Right:28,Bottom:24}:{Left:31,Top:19,Right:23,Bottom:17});
 assert.deepEqual(state.BorderThickness,index<5?{Left:1,Top:1,Right:1,Bottom:1}:{Left:2,Top:3,Right:4,Bottom:5});
 assert.deepEqual(state.CornerRadius,index<5?{TopLeft:12,TopRight:12,BottomRight:12,BottomLeft:12}:{TopLeft:6,TopRight:8,BottomRight:10,BottomLeft:12});
 const focus=report.focusTrace.filter(event=>event.id===state.id);assert.deepEqual(focus.map(event=>event.kind),['before-pointer','after-pointer','focus-requested','focus-presented']);
 assert.equal(focus[0].focused,'Button');assert.equal(focus[0].IsTabStop,false);assert.equal(focus[1].focused,'Page');assert.equal(focus[1].IsTabStop,true);
 assert.equal(focus[3].focused,'Button');assert.equal(focus[3].IsTabStop,false);
}
const clicks=input.pointerClicks.filter(click=>click.target==='PART_Portal');assert.equal(clicks.length,10);
for(const click of clicks)assert(click.delivered&&click.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));
for(const gate of input.pointerPresentations)assert(gate.passed&&gate.stable>=2&&gate.milliseconds<2000);
const captures=[];
for(const scheme of['light','dark']){
 const bytes=await fs.readFile(path.join(native,`catalog-${scheme}.png`)),png=readRgbaPng(bytes);assert.equal(png.width,640);assert.equal(png.height,480);
 captures.push({scheme,sha256:crypto.createHash('sha256').update(bytes).digest('hex')});
}
assert.notEqual(captures[0].sha256,captures[1].sha256);
const audit={passed:true,componentAccepted:false,phases:10,physicalPointerBarriers:10,focusTraceEvents:report.focusTrace.length,callerTemplateAndRestoration:true,instanceThemeResources:true,liveBrushIdentity:true,replacedDictionaryAfterThemeChange:true,localPropertyPrecedence:true,liveTypographyAndDirection:true,originalFocusPreserved:true,rootTabStopRestored:true,captures,sourcePixelParityVerified:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
