import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const directory=process.argv[2];
if(!directory)throw Error('Pass the native data-table-toolbar-interactions output directory.');
const read=async name=>JSON.parse((await fs.readFile(path.join(directory,name),'utf8')).replace(/^\uFEFF/,''));
const referenceBytes=await fs.readFile(new URL('../tests/Reference/DataTableToolbarLayout.json',import.meta.url));
const reference=JSON.parse(referenceBytes),referenceSha256=crypto.createHash('sha256').update(referenceBytes).digest('hex');
const runtime=await read('result.json');assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-toolbar-interactions');
assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);
const captures=new Set(['catalog-light.png','catalog-dark.png']),summary=[];
let gates=0,moves=0,keys=0,clicks=0,modifiers=0,releases=0,maxPointerMilliseconds=0;
for(const scheme of ['Light','Dark']){
 const matrix=await read(`data-table-toolbar-matrix-${scheme}.json`);
 assert.equal(matrix.format,'orchard-data-table-toolbar-native-interactions-v1');assert.equal(matrix.scheme,scheme);assert.equal(matrix.referenceSha256,referenceSha256);
 const cases=reference.cases.filter(item=>item.scheme===scheme&&item.states.length);
 const expected=cases.flatMap(sample=>sample.states.map((step,index)=>({sample,step,index:index+1})));
 assert.equal(matrix.caseCount,cases.length);assert.equal(matrix.stateCount,expected.length);assert.equal(matrix.reports.length,expected.length);
 const resets=new Map();
 for(const [index,{sample,step,index:stepNumber}]of expected.entries()){
  const native=matrix.reports[index];assert.equal(native.id,sample.id);assert.equal(native.step,stepNumber);assert.equal(native.action,step.action);assert.deepEqual(native.errors,[]);
  assert.equal(native.table,step.state.table.name);assert.equal(native.VisibleText,step.state.visibleRows);assert.equal(native.filters,step.state.table.state.columnFilters.length);
  const resetKey=`${sample.id}/${step.state.table.name}`;const count=(resets.get(resetKey)??0)+(['reset-release','reset-click','reset-keyboard','reset-Enter'].includes(step.action)?1:0);resets.set(resetKey,count);assert.equal(native.resets,count);
  const calls=step.state.events.filter(event=>event.kind==='slot');assert.equal(native.slotCallbacks.length,calls.length);
  for(const [callIndex,call]of native.slotCallbacks.entries()){
   assert.equal(call.Role,calls[callIndex].slot);assert.equal(call.Index,calls[callIndex].index);assert.equal(call.Table,calls[callIndex].table);
   for(const field of ['OriginalTable','OriginalRows','OriginalNode'])assert.equal(call[field],true);
  }
  assert.equal(native.sourceFocus,step.state.focused);assert.equal(native.nativeFocus,native.focus.role);assert.equal(native.sourceFocusMatched,native.sourceFocus===native.nativeFocus);
  if(native.focus.nativeHostBoundary){assert.equal(native.focus.name,'OutsideToolbar');assert.equal(native.focus.type,'Button');assert.equal(native.nativeFocus,'body');}
  const name=`data-table-toolbar-matrix-${sample.id}-${String(stepNumber).padStart(2,'0')}-${step.action}-${scheme}.png`;
  assert.equal(native.capture,name);captures.add(name);const png=await fs.readFile(path.join(directory,name));
  assert.equal(png.subarray(0,8).toString('hex'),'89504e470d0a1a0a');assert.equal(png.readUInt32BE(16),sample.viewportWidth);assert.equal(png.readUInt32BE(20),745);
  for(const [axis,field]of [['width','Width'],['height','Height']])assert.ok(Math.abs(native.surface[field]-step.state.surface.box[axis])<=1/32);
  assert.ok(native.surface.X>=0&&native.surface.Y>=0&&native.surface.Right<=sample.viewportWidth&&native.surface.Bottom<=745);
  if(step.action==='reset-release'){
   const release=await read(`data-table-toolbar-matrix-release-${sample.id}-${stepNumber}-${scheme}.json`);
   assert.equal(release.receipts,1);assert.equal(release.exact,true);releases++;
  }
 }
 const differences=matrix.reports.filter(item=>!item.sourceFocusMatched).map(({id,step,action,sourceFocus,nativeFocus})=>({id,step,action,sourceFocus,nativeFocus}));
 assert.deepEqual(matrix.focusDifferences,differences);
 const expectedTabs=expected.filter(item=>item.step.action==='Tab');assert.equal(matrix.transfers.length,expectedTabs.length);
 for(const [index,transfer]of matrix.transfers.entries()){
  assert.equal(transfer.id,expectedTabs[index].sample.id);assert.equal(transfer.step,expectedTabs[index].index);assert.equal(transfer.sourceRole,expectedTabs[index].step.state.focused);
  assert.equal(transfer.downs,1);assert.equal(transfer.ups,1);assert.equal(transfer.downOwned,true);assert.equal(transfer.upOwned,true);
 }
 const desktop=await read(`data-table-toolbar-matrix-${scheme}-desktop.json`);let lastExact;
 for(const gate of desktop.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2);assert.ok(gate.milliseconds<2000);maxPointerMilliseconds=Math.max(maxPointerMilliseconds,gate.milliseconds);gates++;}
 for(const move of desktop.pointerMoves){
  assert.notEqual(move.deliveryFailed,true);assert.equal(move.positioned,true);assert.equal(move.actual.X,move.requested.x);assert.equal(move.actual.Y,move.requested.y);
  assert.equal(move.hit.root.handle,move.hit.own.handle);const exact=move.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated);
  const point=`${move.requested.x},${move.requested.y}`;
  if(move.stationary)assert.ok(lastExact===point||exact);else{assert.equal(move.delivered,true);assert.equal(exact,true);}
  if(exact)lastExact=point;moves++;
 }
 for(const key of desktop.keyActions){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);keys++;
  if(key.modifier!==undefined){assert.equal(key.modifierDowns,1);assert.equal(key.modifierUps,1);modifiers++;}}
 for(const click of desktop.pointerClicks){assert.equal(click.delivered,true);clicks++;}
 const final=await read(`catalog-${scheme.toLowerCase()}.png.layout.json`);assert.ok(final.stable>=2);
 const finalPng=await fs.readFile(path.join(directory,`catalog-${scheme.toLowerCase()}.png`));assert.equal(finalPng.readUInt32BE(16),1024);assert.equal(finalPng.readUInt32BE(20),745);
 summary.push({scheme,cases:cases.length,states:expected.length,tabTransfers:matrix.transfers.length,focusDifferences:differences});
}
const actual=new Set((await fs.readdir(directory)).filter(name=>name.endsWith('.png')));assert.deepEqual([...actual].sort(),[...captures].sort());assert.equal(captures.size,378);
assert.equal(summary.reduce((sum,item)=>sum+item.cases,0),52);assert.equal(summary.reduce((sum,item)=>sum+item.states,0),376);
const report={format:'orchard-data-table-toolbar-native-interactions-audit-v1',checkedGatesPassed:true,nativeComponentAccepted:false,referenceSha256,
 captures:captures.size,pointerGates:gates,pointerMoves:moves,maxPointerMilliseconds,keys,modifiedKeys:modifiers,clicks,heldReleases:releases,results:summary};
await fs.writeFile(path.join(directory,'interaction-audit.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
