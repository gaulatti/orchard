import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const source=option('--source'),native=option('--native'),output=option('--output');
const hash=value=>crypto.createHash('sha256').update(value).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTableFiltersLive.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
assert.equal(referenceSha256,'9d63fbc158c57cd970dbe3d7c72a1fc654580ae9db7fc6c89f72b8f40c530ef3');
assert.equal(hash(await fs.readFile(new URL('../tests/Reference/DataTableFilters.json',import.meta.url))),'f85940d22b27c187d9c14ae7b2a6ce6fc788fb9e3a52971cf3aff6d2575790d6');
assert.equal(hash(await fs.readFile(path.join(source,'reference.json'))),referenceSha256);
for(const[field,file]of[['scriptSha256','capture-data-table-filters-live.cjs'],['clientSha256','data-table-filters-client.tsx']])assert.equal(reference[field],hash(await fs.readFile(new URL('./reference/'+file,import.meta.url))));
const runtime=await read(path.join(native,'result.json'));assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-filters-live');
for(const[field,value]of[['displayScale',1],['textScale',1],['animationsEnabled',false],['highContrast',false]])assert.equal(runtime.environment[field],value);
let states=0,keys=0,mutations=0,gates=0,moves=0,stationaryMoves=0,clicks=0;const images=[],pixels=[],focusIdentityDifferences=[];
for(const scheme of ['Light','Dark']){
 const report=await read(path.join(native,`data-table-filters-live-${scheme}.json`));assert.equal(report.passed,true);assert.equal(report.liveOptions,true);assert.equal(report.desktopFocusContract,false);assert.equal(report.sourceFocusParityVerified,true);assert.equal(report.nativeComponentAccepted,false);assert.equal(report.referenceSha256,referenceSha256);assert.deepEqual(report.failures,[]);assert.deepEqual(report.focusDifferences,[]);
 let index=0;
 for(const sample of reference.cases.filter(item=>item.scheme===scheme)){
  let previous=null;
  for(const point of sample.snapshots){
   const actual=report.reports[index++],state=point.state;assert.equal(actual.id,sample.id);assert.equal(actual.phase,point.phase);assert.deepEqual(actual.errors,[]);assert.equal(actual.sourceFocus,state.focused);assert.equal(actual.expectedFocus,state.focused);assert.equal(actual.nativeFocusPolicy,false);if(point.phase!=='closed')assert.equal(actual.focus,state.focused);
   assert.equal(actual.callbacks,state.events.length);assert.equal(actual.outsideClicks,0);assert.equal(actual.rows,state.visibleRows);
   if(sample.updates[point.phase]){
    assert.equal(actual.callbacks,previous.callbacks);assert.equal(actual.rows,previous.rows);assert.equal(actual.callbacks,0);
    assert.equal(typeof actual.focusRetained,'boolean');assert.equal(typeof actual.priorFocusLoaded,'boolean');
    assert.equal(actual.priorFocusLoaded,point.focusIdentity.previousConnected);
    if(actual.focusRetained!==point.focusIdentity.retained)focusIdentityDifferences.push({id:sample.id,scheme,phase:point.phase,sourceRetained:point.focusIdentity.retained,nativeRetained:actual.focusRetained,sourcePreviousConnected:point.focusIdentity.previousConnected,nativePreviousLoaded:actual.priorFocusLoaded});
    mutations++;
   }
   const key={down:40,home:36,end:35,enter:13,escape:27,tab:9}[point.phase];
   if(key){const receipt=actual.receipt;assert.equal(receipt.key,key);assert.equal(receipt.downs,1);assert.equal(receipt.ups,1);assert.equal(receipt.downOwned,true);assert.equal(receipt.upOwned,true);assert.equal(receipt.shift,false);assert.equal(receipt.shiftDowns,0);assert.equal(receipt.shiftUps,0);assert.equal(receipt.shiftOwned,true);assert.deepEqual(receipt.order,['keydown','keyup']);assert.ok(receipt.downMilliseconds>=0&&receipt.upMilliseconds>=receipt.downMilliseconds&&receipt.upMilliseconds<2000);keys++;}
   else assert.equal(actual.receipt,null);
   const sourceBytes=await fs.readFile(path.join(source,point.file)),nativeBytes=await fs.readFile(path.join(native,actual.file));assert.equal(hash(sourceBytes),point.sha256);const original=readRgbaPng(sourceBytes),painted=readRgbaPng(nativeBytes);assert.deepEqual([painted.width,painted.height],[original.width,original.height]);assert.deepEqual([original.width,original.height],[point.width,point.height]);
   const trigger=actual.geometry.trigger;for(const[field,key]of[['X','x'],['Y','y'],['Width','width'],['Height','height']])assert.ok(Math.abs(trigger[field]-state.trigger.box[key])<=1/32);
   if(state.popupOpen){
    const box=actual.geometry.popup,expected=state.popup.box;for(const[field,key]of[['X','x'],['Y','y'],['Width','width'],['Height','height']])assert.ok(Math.abs(box[field]-expected[key])<=1/32);
    assert.ok(Math.abs(actual.geometry.VerticalOffset-state.viewport.scrollTop)<=1/32);
    assert.deepEqual(actual.geometry.options.map(item=>[item.text,item.IsEnabled]),state.options.map(item=>[item.label,item.disabled!=='true']));
    const left=Math.floor(Math.min(box.Left,expected.x)),top=Math.floor(Math.min(box.Top,expected.y)),right=Math.ceil(Math.max(box.Right,expected.x+expected.width)),bottom=Math.ceil(Math.max(box.Bottom,expected.y+expected.height));assert.ok(left>=0&&top>=0&&right<=original.width&&bottom<=original.height);
    let differences=0;for(let y=top;y<bottom;y++)for(let x=left;x<right;x++){const at=(y*original.width+x)*4;for(let c=0;c<4;c++)if(original.rgba[at+c]!==painted.rgba[at+c]){differences++;break;}}
    pixels.push({id:sample.id,scheme,phase:point.phase,left,top,width:right-left,height:bottom-top,differences});
   }
   images.push({id:sample.id,scheme,phase:point.phase,sourceFile:point.file,sourceSha256:point.sha256,nativeFile:actual.file,nativeSha256:hash(nativeBytes)});previous=actual;states++;
  }
  const input=await read(path.join(native,`filters-live-${sample.id}-${scheme}-desktop.json`));let lastExact=null;
  for(const gate of input.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2&&gate.milliseconds<2000);gates++;}
  for(const move of input.pointerMoves){assert.equal(move.deliveryFailed,undefined);assert.equal(move.positioned,true);assert.equal(move.hit.root.handle,move.hit.own.handle);assert.deepEqual([move.actual.X,move.actual.Y],[move.requested.x,move.requested.y]);const point=[move.requested.x,move.requested.y],exact=move.receipts.some(item=>item.exact&&!item.IsGenerated);if(move.stationary){assert.ok(exact||JSON.stringify(lastExact)===JSON.stringify(point));stationaryMoves++;}else{assert.equal(move.delivered,true);assert.equal(exact,true);}if(exact)lastExact=point;moves++;}
  for(const click of input.pointerClicks){assert.equal(click.delivered,true);clicks++;}
 }
 assert.equal(index,report.reports.length);
 const lifecycle=await read(path.join(native,`data-table-filters-lifecycle-${scheme}.json`));assert.equal(lifecycle.passed,true);assert.equal(lifecycle.originalRequests,1);assert.equal(lifecycle.replacementRequests,0);for(const field of ['originalRows','originalColumn','keyedNativeIdentityRetained','staleOptionRejected','liveLabelRetainedFocus','liveValueInvalidatesProvider','liveDisabledFocusedRejected','disabledOptionRejected','disabledFilterRejected','detachedTemplateRejected','templateReplaced','tableReplaced','remounted'])assert.equal(lifecycle[field],true);
 const unload=await read(path.join(native,`data-table-filters-unload-${scheme}.json`));assert.equal(unload.passed,true);assert.equal(unload.finalObservers,0);
}
assert.equal(reference.cases.length,32);assert.equal(states,140);assert.equal(keys,44);assert.equal(mutations,32);assert.equal(pixels.length,76);assert.equal((await fs.readdir(native)).filter(file=>file.endsWith('.png')).length,142);
assert.deepEqual(focusIdentityDifferences,[]);
const report={format:'orchard-data-table-filters-live-native-audit-v1',referenceSha256,verifierSha256:hash(await fs.readFile(new URL(import.meta.url))),checkedNativeGatesPassed:true,nativeComponentAccepted:false,sourceSettledFocusParityVerified:true,initialUnfocusedStatesExcluded:32,rawFocusIdentityParityVerified:focusIdentityDifferences.length===0,sourceRouteParityVerified:false,pixelParityVerified:pixels.every(item=>item.differences===0),tolerance:0,masks:0,states,keys,mutations,gates,moves,stationaryMoves,clicks,captures:142,focusIdentityDifferences,pixels,images};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,focusIdentityDifferences:report.focusIdentityDifferences.length,pixels:undefined,images:undefined}));
