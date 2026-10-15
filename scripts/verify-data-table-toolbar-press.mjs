import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const directory=process.argv[2];if(!directory)throw Error('Pass the native toolbar press output directory.');
const read=async file=>JSON.parse((await fs.readFile(path.join(directory,file),'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTableToolbarPress.json',import.meta.url)),source=JSON.parse(bytes),referenceSha256=crypto.createHash('sha256').update(bytes).digest('hex');
assert.equal(source.results.length,24);assert.equal(source.results.reduce((sum,item)=>sum+item.states.length,0),108);
const runtime=await read('result.json');assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-toolbar-press');
for(const field of ['displayScale','textScale'])assert.equal(runtime.environment[field],1);
assert.equal(runtime.environment.highContrast,false);assert.equal(runtime.environment.animationsEnabled,false);
const captures=new Set(['catalog-light.png','catalog-dark.png']);let states=0,heldKeys=0,transfers=0,releases=0;
for(const scheme of ['Light','Dark']){
 const report=await read(`data-table-toolbar-press-${scheme}.json`),cases=source.results.filter(item=>item.scheme===scheme&&item.reducedMotion==='reduce');
 assert.equal(report.format,'orchard-data-table-toolbar-native-press-v1');assert.equal(report.scheme,scheme);assert.equal(report.passed,true);assert.equal(report.nativeComponentAccepted,false);assert.equal(report.referenceSha256,referenceSha256);assert.deepEqual(report.failures,[]);
 const expected=cases.flatMap(item=>item.states.map((point,index)=>({id:item.id,step:index+1,...point.state})));assert.equal(report.reports.length,27);assert.equal(expected.length,27);
 for(const [index,point]of expected.entries()){
  const actual=report.reports[index];assert.equal(actual.id,point.id);assert.equal(actual.phase,point.phase);assert.equal(actual.step,point.step);assert.deepEqual(actual.errors,[]);
  assert.equal(actual.file,`data-table-toolbar-press-${point.id}-${point.phase}-${scheme}.png`);assert.ok(!captures.has(actual.file));captures.add(actual.file);states++;
  assert.equal(actual.pointerHit,point.pointerOwnsReset);assert.equal(actual.callbacks,point.clicks.length);assert.equal(actual.focusRole,point.focusRole==='reset'?'BUTTON':point.focusRole==='search'?'INPUT':point.focusRole);
  for(const key of ['scaleX','scaleY'])assert.ok(Math.abs(actual[key]-Number(point.scale))<1e-6);
  assert.ok(Math.abs(actual.translateY-(point.translate==='0px'?0:1))<1e-6);
  for(const key of ['x','y','width','height'])assert.ok(Math.abs(actual.bounds[key]-point.box[key])<=1/32);
  assert.equal(actual.surface.width,800);assert.equal(actual.surface.height,36);
  if(point.pointerOwnsReset){assert.equal(actual.paint.A,scheme==='Light'?31:255);assert.equal(actual.paint.R,scheme==='Light'?230:255);assert.equal(actual.paint.G,scheme==='Light'?213:255);assert.equal(actual.paint.B,scheme==='Light'?184:255);assert.ok(Math.abs(actual.paint.Opacity-(scheme==='Light'?1:.06))<1e-6);}
  else assert.equal(actual.paint.A*actual.paint.Opacity,0);
 }
 assert.equal(report.transfers.length,13);transfers+=report.transfers.length;
 for(const transfer of report.transfers){assert.equal(transfer.downs,1);assert.equal(transfer.ups,1);assert.equal(transfer.downOwned,true);assert.equal(transfer.upOwned,true);if(transfer.reverse){assert.equal(transfer.shiftDowns,1);assert.equal(transfer.shiftUps,1);}}
 assert.equal(report.transfers.filter(item=>item.reverse).length,1);
 const keys=await read(`data-table-toolbar-press-${scheme}-held-keys.json`);assert.equal(keys.length,5);assert.equal(keys.filter(item=>item.focusTransferred).length,1);
 for(const key of keys){assert.ok([0x0d,0x20].includes(key.key));assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);assert.equal(key.downTarget,'PART_Reset');heldKeys++;}
 for(const key of ['drag-out','drag-return','enter-outside-restored']){const release=await read(`data-table-toolbar-matrix-release-press-${key}-${scheme}.json`);assert.equal(release.receipts,1);assert.equal(release.exact,true);releases++;}
 const desktop=await read(`data-table-toolbar-press-${scheme}-desktop.json`);
 for(const gate of desktop.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2);assert.ok(gate.milliseconds<2000);}
 let lastExact=null;
 for(const move of desktop.pointerMoves){
  const point=[move.requested.x,move.requested.y];assert.equal(move.positioned,true);assert.deepEqual([move.actual.X,move.actual.Y],point);assert.equal(move.hit.root.handle,move.hit.own.handle);assert.equal(move.hit.foreground.handle,move.hit.own.handle);
  if(move.receipts.some(item=>item.exact&&!item.IsGenerated))lastExact=point;
  else{assert.equal(move.stationary,true);assert.deepEqual(point,lastExact,'Stationary move lacks a prior exact receipt at this point.');}
 }
}
assert.equal(captures.size,56);assert.deepEqual(new Set((await fs.readdir(directory)).filter(file=>file.endsWith('.png'))),captures);
for(const file of captures){const png=await fs.readFile(path.join(directory,file));assert.equal(png.subarray(0,8).toString('hex'),'89504e470d0a1a0a');assert.equal(png.readUInt32BE(16),1024);assert.equal(png.readUInt32BE(20),745);}
const summary={format:'orchard-data-table-toolbar-native-press-audit-v1',checkedGatesPassed:true,nativeComponentAccepted:false,referenceSha256,states,captures:captures.size,heldKeys,tabTransfers:transfers,exactPointerReleases:releases};
await fs.writeFile(path.join(directory,'press-audit.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify(summary));
