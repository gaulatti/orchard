import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const sourceDirectory=option('--source'),nativeDirectory=option('--native'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTablePaginationKeyboard.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory,'reference.json'))),referenceSha256);
assert.equal(reference.scriptSha256,hash(await fs.readFile(new URL('./reference/capture-data-table-pagination-keyboard.cjs',import.meta.url))));
assert.equal(reference.baselineSha256,hash(await fs.readFile(new URL('../tests/Reference/DataTablePagination.json',import.meta.url))));
assert.equal(reference.clientSha256,hash(await fs.readFile(new URL('./reference/data-table-pagination-client.tsx',import.meta.url))));
const runtime=await read(path.join(nativeDirectory,'result.json'));assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-pagination-keyboard');
assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(runtime.environment.highContrast,false);
let states=0,keys=0,heldKeys=0,reverseTabs=0,pointerGates=0;const captures=[];
for(const scheme of ['Light','Dark']){
 const native=await read(path.join(nativeDirectory,`data-table-pagination-keyboard-${scheme}.json`));assert.equal(native.passed,true);assert.equal(native.referenceSha256,referenceSha256);assert.deepEqual(native.failures,[]);
 const expected=reference.cases.filter(item=>item.scheme===scheme).flatMap(item=>item.snapshots.map(point=>({id:item.id,width:item.viewportWidth,height:item.viewportHeight,...point})));
 assert.equal(expected.length,native.reports.length);
 for(const [index,point]of expected.entries()){
  const actual=native.reports[index];assert.equal(actual.id,point.id);assert.equal(actual.phase,point.phase);assert.deepEqual(actual.errors,[]);assert.equal(actual.expectedRole,point.state.focused);assert.equal(actual.popupOpen,point.state.popupOpen);assert.equal(actual.capture,'desktop-client');
  assert.deepEqual([actual.PageIndex,actual.PageSize,actual.PageCount],[point.state.table.pagination.pageIndex,point.state.table.pagination.pageSize,point.state.table.pageCount]);assert.equal(actual.callbacks,point.state.events.length);assert.equal(actual.probe,point.state.visibleRows);
  assert.equal(point.presentation.stable>=3,true);assert.equal(point.presentation.milliseconds<2000,true);assert.equal(point.state.inputEvents.every(event=>event.trusted),true);
  if(point.key){const key=actual.receipt;assert.equal(key.key,point.key);assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);keys++;
   if(point.key==='Shift+Tab'){reverseTabs++;assert.equal(key.shiftDowns,1);assert.equal(key.shiftUps,1);assert.equal(key.shiftOwned,true);}else{assert.equal(key.shiftDowns,0);assert.equal(key.shiftUps,0);}
  }else assert.equal(actual.receipt,null);
  const sourceBytes=await fs.readFile(path.join(sourceDirectory,point.file));assert.equal(hash(sourceBytes),point.sha256);const sourcePng=readRgbaPng(sourceBytes);assert.deepEqual([sourcePng.width,sourcePng.height],[point.width,point.height]);
  const nativeBytes=await fs.readFile(path.join(nativeDirectory,actual.file)),nativePng=readRgbaPng(nativeBytes);assert.deepEqual([nativePng.width,nativePng.height],[point.width,point.height]);
  captures.push({id:point.id,scheme,phase:point.phase,sourceFile:point.file,sourceSha256:point.sha256,nativeFile:actual.file,nativeSha256:hash(nativeBytes)});states++;
 }
 for(const sample of reference.cases.filter(item=>item.scheme===scheme)){
  const prefix=`pagination-keyboard-${sample.id}-${scheme}`,held=await read(path.join(nativeDirectory,prefix+'-held-keys.json'));
  assert.equal(held.length,sample.snapshots.filter(point=>point.key&&point.key!=='Shift+Tab').length);
  for(const pair of held){assert.equal(pair.downs,1);assert.equal(pair.ups,1);assert.equal(pair.downOwned,true);assert.equal(pair.upOwned,true);heldKeys++;}
  const desktop=await read(path.join(nativeDirectory,prefix+'-desktop.json'));assert.equal(desktop.pointerPresentations.length,1);assert.equal(desktop.pointerClicks.length,1);
  for(const gate of desktop.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2&&gate.milliseconds<2000);pointerGates++;}
  for(const move of desktop.pointerMoves){assert.equal(move.hit.root.handle,move.hit.own.handle);assert.equal(move.delivered,true);assert.ok(move.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));}
 }
}
assert.equal(states,136);assert.equal(keys,120);assert.equal(heldKeys,118);assert.equal(reverseTabs,2);assert.equal(pointerGates,16);
const files=(await fs.readdir(nativeDirectory)).filter(name=>name.endsWith('.png'));assert.equal(files.length,138);assert.equal(new Set(captures.map(item=>item.nativeFile)).size,136);assert.ok(files.includes('catalog-light.png')&&files.includes('catalog-dark.png'));
const report={format:'orchard-data-table-pagination-keyboard-audit-v1',referenceSha256,checkedGatesPassed:true,nativeComponentAccepted:false,states,keys,heldKeys,reverseTabs,pointerGates,captures:files.length,zeroDelayKeyPairsVerified:false,pixelParityVerified:false,images:captures};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,images:undefined}));
