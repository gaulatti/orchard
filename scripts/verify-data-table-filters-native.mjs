import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const source=option('--source'),native=option('--native'),output=option('--output'),hash=value=>crypto.createHash('sha256').update(value).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTableFilters.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
assert.equal(hash(await fs.readFile(path.join(source,'reference.json'))),referenceSha256);
for(const[field,file]of[['scriptSha256','capture-data-table-filters.cjs'],['clientSha256','data-table-filters-client.tsx']])assert.equal(reference[field],hash(await fs.readFile(new URL('./reference/'+file,import.meta.url))));
const runtime=await read(path.join(native,'result.json'));assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-filters-native');assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(runtime.environment.highContrast,false);
let states=0,keys=0,reverseTabs=0,moves=0,stationaryMoves=0,clicks=0,gates=0;const images=[],pixelComparisons=[],focusDifferences=[],modalReceipts=[];
for(const scheme of ['Light','Dark']){
 const report=await read(path.join(native,`data-table-filters-${scheme}.json`));assert.equal(report.passed,true);assert.equal(report.desktopFocusContract,true);assert.equal(report.nativeComponentAccepted,false);assert.equal(report.sourceFocusParityVerified,false);assert.equal(report.referenceSha256,referenceSha256);assert.deepEqual(report.failures,[]);assert.equal(report.focusDifferences.length,2);focusDifferences.push(...report.focusDifferences.map(item=>({scheme,...item})));
 const expected=reference.cases.filter(item=>item.scheme===scheme).flatMap(sample=>sample.snapshots.map(point=>({id:sample.id,...point})));assert.equal(report.reports.length,expected.length);let removalObserved=false;
 for(const[index,point]of expected.entries()){
  const actual=report.reports[index],state=point.state;assert.equal(actual.id,point.id);assert.equal(actual.phase,point.phase);assert.deepEqual(actual.errors,[]);assert.equal(actual.sourceFocus,state.focused);assert.equal(actual.callbacks,state.events.length);assert.equal(actual.outsideClicks,state.outsideClicks);assert.equal(actual.rows,state.visibleRows);
  if(point.id==='missing'&&point.phase==='remove-column')removalObserved=true;
  const policy=point.id==='missing'&&removalObserved&&['remove-column','restore-column'].includes(point.phase)&&state.focused==='BODY';assert.equal(actual.nativeFocusPolicy,policy);assert.equal(actual.expectedFocus,policy?'outside':state.focused);if(point.phase!=='closed')assert.equal(actual.focus,actual.expectedFocus);
  const key={down:40,home:36,end:35,enter:13,escape:27,tab:9,'reverse-tab':9,'type-p':80,'type-d':68,focus:9}[point.phase];
  if(key){const receipt=actual.receipt;assert.equal(receipt.key,key);assert.equal(receipt.downs,1);assert.equal(receipt.ups,1);assert.equal(receipt.downOwned,true);assert.equal(receipt.upOwned,true);assert.equal(receipt.shift,point.phase==='reverse-tab');assert.equal(receipt.shiftDowns,receipt.shift?1:0);assert.equal(receipt.shiftUps,receipt.shift?1:0);assert.equal(receipt.shiftOwned,true);assert.deepEqual(receipt.order,receipt.shift?['shift-down','keydown','keyup','shift-up']:['keydown','keyup']);assert.ok(receipt.downMilliseconds>=0&&receipt.upMilliseconds>=receipt.downMilliseconds);keys++;if(receipt.shift)reverseTabs++;}
  else if(point.phase!=='outside')assert.equal(actual.receipt,null);
  const originalBytes=await fs.readFile(path.join(source,point.file)),paintedBytes=await fs.readFile(path.join(native,actual.file));assert.equal(hash(originalBytes),point.sha256);const original=readRgbaPng(originalBytes),painted=readRgbaPng(paintedBytes);assert.deepEqual([original.width,original.height],[point.width,point.height]);assert.deepEqual([painted.width,painted.height],[point.width,point.height]);
  if(state.popupOpen){const box=actual.geometry.popup,ref=state.popup.box;for(const[key,field]of[['X','x'],['Y','y'],['Width','width'],['Height','height']])assert.ok(Math.abs(box[key]-ref[field])<=1/32);const left=Math.floor(Math.min(box.Left,ref.x)),top=Math.floor(Math.min(box.Top,ref.y)),right=Math.ceil(Math.max(box.Right,ref.x+ref.width)),bottom=Math.ceil(Math.max(box.Bottom,ref.y+ref.height));assert.ok(left>=0&&top>=0&&right<=original.width&&bottom<=original.height);let differences=0;for(let y=top;y<bottom;y++)for(let x=left;x<right;x++){const at=(y*original.width+x)*4;for(let c=0;c<4;c++)if(original.rgba[at+c]!==painted.rgba[at+c]){differences++;break;}}pixelComparisons.push({id:point.id,scheme,phase:point.phase,left,top,width:right-left,height:bottom-top,differences});}
  images.push({id:point.id,scheme,phase:point.phase,sourceFile:point.file,sourceSha256:point.sha256,nativeFile:actual.file,nativeSha256:hash(paintedBytes)});states++;
 }
 for(const sample of reference.cases.filter(item=>item.scheme===scheme)){
  const input=await read(path.join(native,`filters-${sample.id}-${scheme}-desktop.json`));
  for(const gate of input.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2&&gate.milliseconds<2000);gates++;}
  let lastExact=null;
  for(const move of input.pointerMoves){
   assert.equal(move.deliveryFailed,undefined);assert.equal(move.hit.root.handle,move.hit.own.handle);assert.equal(move.positioned,true);assert.deepEqual([move.actual.X,move.actual.Y],[move.requested.x,move.requested.y]);
   const point=[move.requested.x,move.requested.y],exact=move.receipts.some(item=>item.exact&&!item.IsGenerated);
   if(move.stationary){assert.ok(exact||JSON.stringify(lastExact)===JSON.stringify(point),'Stationary pointer requires a prior exact receipt at this point in this input session.');stationaryMoves++;}
   else {assert.equal(move.delivered,true);assert.equal(exact,true);}
   if(exact)lastExact=point;moves++;
  }
  for(const click of input.pointerClicks){assert.equal(click.delivered,true);clicks++;}
 }
 const modal=await read(path.join(native,`filters-default-${scheme}-native-modal-input-source.json`));assert.equal(modal.passed,true);assert.equal(modal.layer,'winui-input-pointer-source');assert.equal(modal.routedXamlReceiptsClaimed,false);assert.equal(modal.handlersDetached,true);assert.deepEqual([modal.moves,modal.downs,modal.ups],[1,1,1]);assert.deepEqual(modal.order,['move','down','up']);assert.deepEqual(modal.errors,[]);assert.equal(modal.receipts.length,3);
 let previousFrame=0,previousTime=0;for(const item of modal.receipts){assert.equal(item.exact,true);assert.equal(item.exactSource,true);assert.equal(item.island,modal.island);assert.equal(item.PointerId,modal.receipts[0].PointerId);assert.ok(item.FrameId>previousFrame&&item.Timestamp>=previousTime);assert.equal(item.actualX,modal.x);assert.equal(item.actualY,modal.y);assert.equal(item.IsLeftButtonPressed,item.kind==='down');assert.equal(item.IsInContact,item.kind==='down');previousFrame=item.FrameId;previousTime=item.Timestamp;}modalReceipts.push({scheme,...modal});
 const lifecycle=await read(path.join(native,`data-table-filters-lifecycle-${scheme}.json`));assert.equal(lifecycle.passed,true);for(const field of ['originalRows','originalColumn','staleOptionRejected','disabledOptionRejected','disabledFilterRejected','detachedTemplateRejected','templateReplaced','tableReplaced','remounted'])assert.equal(lifecycle[field],true);assert.equal(lifecycle.originalRequests,1);assert.equal(lifecycle.replacementRequests,0);const unload=await read(path.join(native,`data-table-filters-unload-${scheme}.json`));assert.equal(unload.passed,true);assert.equal(unload.finalObservers,0);
}
assert.equal(states,276);assert.equal(keys,80);assert.equal(reverseTabs,2);assert.equal(pixelComparisons.length,114);assert.equal(focusDifferences.length,4);assert.ok(focusDifferences.every(item=>item.id==='missing'&&['remove-column','restore-column'].includes(item.phase)&&item.sourceFocus==='BODY'&&item.nativeFocus==='outside'));
assert.equal((await fs.readdir(native)).filter(name=>name.endsWith('.png')).length,278);
const report={format:'orchard-data-table-filters-native-audit-v1',referenceSha256,verifierSha256:hash(await fs.readFile(new URL(import.meta.url))),checkedDesktopGatesPassed:true,nativeComponentAccepted:false,sourceFocusParityVerified:false,sourceRouteParityVerified:false,pixelParityVerified:pixelComparisons.every(item=>item.differences===0),masks:0,tolerance:0,states,keys,reverseTabs,gates,moves,stationaryMoves,clicks,modalActions:modalReceipts.length,captures:images.length+2,focusDifferences,modalReceipts,pixelComparisons,images};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,modalReceipts:undefined,pixelComparisons:undefined,images:undefined}));
