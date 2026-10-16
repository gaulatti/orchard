import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const sourceDirectory=option('--source'),nativeDirectory=option('--native'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTableColumnToggle.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory,'reference.json'))),referenceSha256);
for(const[field,file]of[['scriptSha256','capture-data-table-column-toggle.cjs'],['clientSha256','data-table-column-toggle-client.tsx']])assert.equal(reference[field],hash(await fs.readFile(new URL('./reference/'+file,import.meta.url))));
const result=await read(path.join(nativeDirectory,'result.json'));assert.equal(result.passed,true);assert.equal(result.component,'data-table-column-toggle');assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.animationsEnabled,false);assert.equal(result.environment.highContrast,false);
const near=(a,b)=>assert.ok(Number.isFinite(a)&&Math.abs(a-b)<=1/32,`${a}/${b}`);
let states=0,keys=0,reverseTabs=0,pointerGates=0,moves=0,clicks=0;const images=[],focusDifferences=[],popupPixels=[];
for(const scheme of ['Light','Dark']){
 const native=await read(path.join(nativeDirectory,`data-table-column-toggle-${scheme}.json`));assert.equal(native.passed,true);assert.equal(native.nativeComponentAccepted,false);assert.equal(native.sourceFocusParityVerified,false);assert.equal(native.referenceSha256,referenceSha256);assert.deepEqual(native.failures,[]);assert.equal(native.focusDifferences.length,2);focusDifferences.push(...native.focusDifferences.map(item=>({scheme,...item})));
 const expected=reference.cases.filter(item=>item.scheme===scheme).flatMap(item=>item.snapshots.map(point=>({id:item.id,direction:item.direction,...point})));
 assert.equal(native.reports.length,expected.length);
 for(const[index,point]of expected.entries()){
  const actual=native.reports[index],source=point.state;assert.equal(actual.id,point.id);assert.equal(actual.phase,point.phase);assert.deepEqual(actual.errors,[]);assert.equal(actual.sourceFocus,source.focused);assert.equal(actual.callbacks,source.events.length);assert.equal(actual.outsideClicks,source.outsideClicks);assert.equal(actual.visibleCells,source.visibleCells);
  if(actual.nativeContainerFocus){assert.equal(point.id,'eligibility');assert.ok(['hide-eligibility','restore-eligibility'].includes(point.phase));assert.equal(source.focused,'BODY');assert.equal(actual.focus,'dialog');}else if(point.phase!=='closed')assert.equal(actual.focus,source.focused);
  assert.ok(point.presentation.stable>=3&&point.presentation.milliseconds<2000);assert.ok(source.inputEvents.every(event=>event.trusted));
  if(['space','tab','reverse-tab','escape'].includes(point.phase)){
   const key=actual.receipt;assert.equal(key.key,{space:32,tab:9,'reverse-tab':9,escape:27}[point.phase]);assert.equal(key.shift,point.phase==='reverse-tab');assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);assert.equal(key.shiftOwned,true);assert.equal(key.shiftDowns,key.shift?1:0);assert.equal(key.shiftUps,key.shift?1:0);assert.deepEqual(key.order,key.shift?['shift-down','keydown','keyup','shift-up']:['keydown','keyup']);assert.ok(Number.isFinite(key.downMilliseconds)&&key.downMilliseconds>=0&&key.upMilliseconds>=key.downMilliseconds);keys++;if(key.shift)reverseTabs++;
  }else assert.equal(actual.receipt,null);
  const sourceBytes=await fs.readFile(path.join(sourceDirectory,point.file)),nativeBytes=await fs.readFile(path.join(nativeDirectory,actual.file));assert.equal(hash(sourceBytes),point.sha256);
  const original=readRgbaPng(sourceBytes),painted=readRgbaPng(nativeBytes);assert.deepEqual([original.width,original.height],[point.width,point.height]);assert.deepEqual([painted.width,painted.height],[point.width,point.height]);
  if(source.popupOpen){
   const box=actual.geometry.popup;near(box.Width,source.popup.box.width);near(box.Height,source.popup.box.height);near(actual.geometry.VerticalOffset,source.viewport.scrollTop);near(actual.geometry.viewport.Height,source.viewport.clientHeight);
   assert.deepEqual(actual.geometry.options.map(item=>[item.column,item.IsChecked]),source.options.map(item=>[item.id,item.checked]));
   for(const check of actual.geometry.options){near(check.box.Width,20);near(check.box.Height,20);}
   // Compare the complete popup rectangle at its actual shared root position.
   // No masks, resampling, palette substitutions or nonzero pixel tolerance.
   const left=Math.floor(Math.min(box.X,source.popup.box.x)),top=Math.floor(Math.min(box.Y,source.popup.box.y)),right=Math.ceil(Math.max(box.Right,source.popup.box.x+source.popup.box.width)),bottom=Math.ceil(Math.max(box.Bottom,source.popup.box.y+source.popup.box.height));
   assert.ok(left>=0&&top>=0&&right<=original.width&&bottom<=original.height);let differing=0;
   for(let y=top;y<bottom;y++)for(let x=left;x<right;x++){const at=(y*original.width+x)*4;for(let c=0;c<4;c++)if(original.rgba[at+c]!==painted.rgba[at+c]){differing++;break;}}
   popupPixels.push({id:point.id,scheme,phase:point.phase,left,top,width:right-left,height:bottom-top,differingPixels:differing});
  }else assert.equal(actual.geometry,null);
  images.push({id:point.id,scheme,phase:point.phase,sourceFile:point.file,sourceSha256:point.sha256,nativeFile:actual.file,nativeSha256:hash(nativeBytes)});states++;
 }
 for(const sample of reference.cases.filter(item=>item.scheme===scheme)){
  const input=await read(path.join(nativeDirectory,`column-toggle-${sample.id}-${scheme}-desktop.json`));
  for(const gate of input.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2&&gate.milliseconds<2000);pointerGates++;}
  for(const move of input.pointerMoves){assert.equal(move.hit.root.handle,move.hit.own.handle);assert.equal(move.delivered,true);assert.ok(move.receipts.some(item=>item.exact&&!item.IsGenerated));moves++;}
  for(const click of input.pointerClicks){assert.equal(click.delivered,true);clicks++;}
 }
 const lifecycle=await read(path.join(nativeDirectory,`data-table-column-toggle-lifecycle-${scheme}.json`));assert.equal(lifecycle.passed,true);for(const key of ['originalColumns','originalRows','disabledRejected','staleTemplateRejected','templateReplaced','remounted'])assert.equal(lifecycle[key],true);assert.equal(lifecycle.visibilityRequests,1);assert.equal(lifecycle.replacementRequests,0);
}
assert.equal(states,132);assert.equal(focusDifferences.length,4);assert.equal(reverseTabs,4);
const files=(await fs.readdir(nativeDirectory)).filter(name=>name.endsWith('.png'));assert.equal(files.length,136);assert.equal(new Set(images.map(item=>item.nativeFile)).size,132);
const report={format:'orchard-data-table-column-toggle-audit-v1',referenceSha256,checkedFunctionalGatesPassed:true,nativeComponentAccepted:false,sourceFocusParityVerified:false,sourceReleaseOwnerParityVerified:false,pixelParityVerified:popupPixels.every(item=>item.differingPixels===0),states,keys,reverseTabs,pointerGates,moves,clicks,captures:files.length,focusDifferences,popupPixels,images};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,popupPixels:undefined,images:undefined}));
