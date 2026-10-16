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
const runtime=await read(path.join(nativeDirectory,'result.json'));assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-pagination-paired');
assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(runtime.environment.highContrast,false);
let states=0,keys=0,reverseTabs=0,pointerGates=0,sourceBodyReleases=0,sourceOptionReleasesBeforeSettledFocus=0,nativeReleasesBeforeSettledFocus=0;const images=[],releaseOwners=[];
for(const scheme of ['Light','Dark']){
 const native=await read(path.join(nativeDirectory,`data-table-pagination-keyboard-${scheme}.json`));assert.equal(native.format,'orchard-data-table-pagination-native-paired-v1');assert.equal(native.pairedKeys,true);assert.equal(native.passed,true);assert.equal(native.referenceSha256,referenceSha256);assert.deepEqual(native.failures,[]);
 const expected=reference.cases.filter(item=>item.scheme===scheme).flatMap(item=>item.snapshots.map(point=>({id:item.id,width:item.viewportWidth,height:item.viewportHeight,...point})));
 assert.equal(expected.length,native.reports.length);
 for(const [index,point]of expected.entries()){
  const actual=native.reports[index];assert.equal(actual.id,point.id);assert.equal(actual.phase,point.phase);assert.deepEqual(actual.errors,[]);assert.equal(actual.expectedRole,point.state.focused);assert.equal(actual.popupOpen,point.state.popupOpen);assert.equal(actual.capture,'desktop-client');
  assert.deepEqual([actual.PageIndex,actual.PageSize,actual.PageCount],[point.state.table.pagination.pageIndex,point.state.table.pagination.pageSize,point.state.table.pageCount]);assert.equal(actual.callbacks,point.state.events.length);assert.equal(actual.probe,point.state.visibleRows);assert.equal(actual.displayedValue,String(point.state.table.pagination.pageSize));
  assert.ok(point.presentation.stable>=3&&point.presentation.milliseconds<2000);assert.ok(point.state.inputEvents.every(event=>event.trusted));
  if(point.key){const key=actual.receipt;assert.equal(key.key,point.key);assert.equal(key.paired,true);assert.equal(key.nativeInputBatchSize,point.key==='Shift+Tab'?4:2);assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.releaseFocusedOwner,true);assert.equal(key.downTarget,key.from);assert.equal(key.upTarget,key.upFocus);
   assert.deepEqual(key.order,point.key==='Shift+Tab'?['shift-down','keydown','keyup','shift-up']:['keydown','keyup']);assert.ok(Number.isFinite(key.downMilliseconds)&&key.downMilliseconds>=0&&Number.isFinite(key.upMilliseconds)&&key.upMilliseconds>=key.downMilliseconds);
   const sourceKey=point.key==='Shift+Tab'?'Tab':point.key==='Space'?' ':point.key;
   const source=point.state.inputEvents.filter(event=>event.key===sourceKey&&['keydown','keyup'].includes(event.type)).slice(-2);assert.deepEqual(key.source,source);assert.deepEqual(source.map(event=>event.type),['keydown','keyup']);
   const released=source[1];if(released.tag==='BODY')sourceBodyReleases++;if(released.role==='option'&&'option:'+released.text!==point.state.focused)sourceOptionReleasesBeforeSettledFocus++;if(key.upTarget!==actual.focus)nativeReleasesBeforeSettledFocus++;
   releaseOwners.push({id:point.id,scheme,phase:point.phase,key:point.key,sourceDown:source[0],sourceUp:released,sourceSettledFocus:point.state.focused,nativeDown:key.downTarget,nativeUp:key.upTarget,nativeFocusAtRelease:key.upFocus,nativeSettledFocus:actual.focus,downMilliseconds:key.downMilliseconds,upMilliseconds:key.upMilliseconds});keys++;
   if(point.key==='Shift+Tab'){reverseTabs++;assert.equal(key.shiftDowns,1);assert.equal(key.shiftUps,1);assert.equal(key.shiftOwned,true);}else{assert.equal(key.shiftDowns,0);assert.equal(key.shiftUps,0);}
  }else assert.equal(actual.receipt,null);
  const sourceBytes=await fs.readFile(path.join(sourceDirectory,point.file)),nativeBytes=await fs.readFile(path.join(nativeDirectory,actual.file));assert.equal(hash(sourceBytes),point.sha256);
  const sourcePng=readRgbaPng(sourceBytes),nativePng=readRgbaPng(nativeBytes);assert.deepEqual([sourcePng.width,sourcePng.height],[point.width,point.height]);assert.deepEqual([nativePng.width,nativePng.height],[point.width,point.height]);
  images.push({id:point.id,scheme,phase:point.phase,sourceFile:point.file,sourceSha256:point.sha256,nativeFile:actual.file,nativeSha256:hash(nativeBytes)});states++;
 }
 for(const sample of reference.cases.filter(item=>item.scheme===scheme)){
  const prefix=`pagination-keyboard-${sample.id}-${scheme}`,desktop=await read(path.join(nativeDirectory,prefix+'-desktop.json'));assert.equal(desktop.pointerPresentations.length,1);assert.equal(desktop.pointerClicks.length,1);
  assert.equal((await fs.readdir(nativeDirectory)).includes(prefix+'-held-keys.json'),false);
  for(const click of desktop.pointerClicks){assert.equal(click.delivered,true);if(click.currentPosition){assert.equal(click.downs,1);assert.equal(click.ups,1);assert.equal(click.downExact,true);assert.equal(click.upExact,true);assert.equal(click.receipts.length,2);assert.ok(click.receipts.every(item=>item.exact&&!item.IsGenerated));}}
  for(const gate of desktop.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2&&gate.milliseconds<2000);pointerGates++;}
  for(const move of desktop.pointerMoves){assert.equal(move.hit.root.handle,move.hit.own.handle);assert.equal(move.delivered,true);assert.ok(move.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));}
 }
 const current=await read(path.join(nativeDirectory,`pagination-keyboard-current-point-${scheme}-desktop.json`));assert.equal(current.pointerMoves.length,0);assert.equal(current.pointerClicks.length,1);assert.equal(current.pointerPresentations.length,1);
 const click=current.pointerClicks[0];assert.equal(click.currentPosition,true);assert.equal(click.downs,1);assert.equal(click.ups,1);assert.equal(click.downExact,true);assert.equal(click.upExact,true);assert.equal(click.delivered,true);assert.equal(click.receipts.length,2);assert.ok(click.receipts.every(item=>item.exact&&!item.IsGenerated));assert.equal(current.pointerPresentations[0].passed,true);assert.ok(current.pointerPresentations[0].stable>=2&&current.pointerPresentations[0].milliseconds<2000);
}
assert.equal(states,136);assert.equal(keys,120);assert.equal(reverseTabs,2);assert.equal(pointerGates,16);
const files=(await fs.readdir(nativeDirectory)).filter(name=>name.endsWith('.png'));assert.equal(files.length,138);assert.equal(new Set(images.map(item=>item.nativeFile)).size,136);assert.ok(files.includes('catalog-light.png')&&files.includes('catalog-dark.png'));
const report={format:'orchard-data-table-pagination-paired-audit-v1',referenceSha256,checkedFunctionalGatesPassed:true,nativeComponentAccepted:false,states,keys,heldKeys:0,reverseTabs,pointerGates,currentPointClicks:2,currentPointMovedEvents:0,captures:files.length,sourceBodyReleases,sourceOptionReleasesBeforeSettledFocus,nativeReleasesBeforeSettledFocus,sourceReleaseOwnerParityVerified:false,pixelParityVerified:false,releaseOwners,images};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,releaseOwners:undefined,images:undefined}));
