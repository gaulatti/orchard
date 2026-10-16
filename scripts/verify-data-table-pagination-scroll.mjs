import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const sourceDirectory=option('--source'),nativeDirectory=option('--native'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTablePaginationScroll.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory,'reference.json'))),referenceSha256);
assert.equal(reference.scriptSha256,hash(await fs.readFile(new URL('./reference/capture-data-table-pagination-scroll.cjs',import.meta.url))));
const keyboardBytes=await fs.readFile(new URL('../tests/Reference/DataTablePaginationKeyboard.json',import.meta.url)),keyboard=JSON.parse(keyboardBytes);
assert.equal(reference.keyboardReferenceSha256,hash(keyboardBytes));
assert.equal(reference.bundleSha256,keyboard.bundleSha256);assert.equal(reference.browser,keyboard.browser);
const runtime=await read(path.join(nativeDirectory,'result.json'));assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-pagination-scroll');
assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(runtime.environment.highContrast,false);
const keyFor={'opened':'Space','end':'End','home':'Home','type-5':'5','type-50':'0','home-after-type':'Home','viewport-next':'ArrowDown','closed':'Escape','closed-type-5':'5','closed-type-50':'0'};
const near=(actual,wanted)=>assert.ok(Number.isFinite(actual)&&Math.abs(actual-wanted)<=1/32,`${actual}/${wanted}`);
let states=0,keys=0,heldKeys=0,pointerGates=0,pointerMoves=0;const images=[];
for(const scheme of ['Light','Dark']){
 const native=await read(path.join(nativeDirectory,`data-table-pagination-scroll-${scheme}.json`));assert.equal(native.passed,true);assert.equal(native.nativeComponentAccepted,false);assert.equal(native.referenceSha256,referenceSha256);assert.deepEqual(native.failures,[]);
 const expected=reference.reports.filter(item=>item.scheme===scheme);assert.equal(expected.length,18);assert.equal(native.reports.length,expected.length);
 for(const [index,point]of expected.entries()){
  const actual=native.reports[index],state=point.state;assert.equal(actual.phase,point.phase);assert.deepEqual(actual.errors,[]);assert.equal(actual.focus,state.focused);assert.equal(actual.callbacks,state.events.length);
  assert.equal(state.table.sameTable,true);assert.equal(state.table.sameRows,true);assert.ok(state.table.rows.every(row=>row.same));assert.ok(state.inputEvents.every(event=>event.trusted));assert.ok(state.receipts.every(event=>event.trusted));
  assert.ok(point.presentation.stable>=3&&point.presentation.milliseconds<2000);
  if(keyFor[point.phase]){const key=actual.receipt;assert.equal(key.key,keyFor[point.phase]);assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);keys++;}else assert.equal(actual.receipt,null);
  if(state.popupOpen){const measured=actual.measured;near(measured.popup.Width,state.popup.width);near(measured.popup.Height,state.popup.height);near(measured.viewport.Width,state.viewport.box.width);near(measured.viewport.Height,state.viewport.box.height);near(measured.VerticalOffset,state.viewport.scrollTop);near(measured.ExtentHeight,state.viewport.scrollHeight);near(measured.ViewportHeight,state.viewport.clientHeight);for(const field of ['up','down'])assert.equal(measured[field],state[field]?'Visible':'Collapsed');}else assert.equal(actual.measured,null);
  const sourceBytes=await fs.readFile(path.join(sourceDirectory,point.file)),nativeBytes=await fs.readFile(path.join(nativeDirectory,actual.file));assert.equal(hash(sourceBytes),point.sha256);
  const sourcePng=readRgbaPng(sourceBytes),nativePng=readRgbaPng(nativeBytes);assert.deepEqual([sourcePng.width,sourcePng.height],[1000,745]);assert.deepEqual([nativePng.width,nativePng.height],[1000,745]);
  images.push({scheme,phase:point.phase,sourceFile:point.file,sourceSha256:point.sha256,nativeFile:actual.file,nativeSha256:hash(nativeBytes)});states++;
 }
 const held=await read(path.join(nativeDirectory,`pagination-scroll-${scheme}-held-keys.json`));assert.equal(held.length,10);for(const key of held){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);heldKeys++;}
 const desktop=await read(path.join(nativeDirectory,`pagination-scroll-${scheme}-desktop.json`));assert.equal(desktop.pointerPresentations.length,6);assert.equal(desktop.pointerMoves.length,6);assert.equal(desktop.pointerClicks.length,0);
 for(const gate of desktop.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2&&gate.milliseconds<2000);pointerGates++;}
 for(const move of desktop.pointerMoves){assert.equal(move.positioned,true);assert.equal(move.delivered,true);assert.equal(move.deliveryFailed,undefined);assert.equal(move.stationary,false);assert.deepEqual([move.actual.X,move.actual.Y],[move.requested.x,move.requested.y]);assert.equal(move.hit.root.handle,move.hit.own.handle);assert.ok(move.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));pointerMoves++;}
}
assert.equal(states,36);assert.equal(keys,20);assert.equal(heldKeys,20);assert.equal(pointerGates,12);assert.equal(pointerMoves,12);
const files=(await fs.readdir(nativeDirectory)).filter(name=>name.endsWith('.png'));assert.equal(files.length,38);assert.equal(new Set(images.map(item=>item.nativeFile)).size,36);assert.ok(files.includes('catalog-light.png')&&files.includes('catalog-dark.png'));
const report={format:'orchard-data-table-pagination-scroll-audit-v1',referenceSha256,checkedGatesPassed:true,nativeComponentAccepted:false,states,keys,heldKeys,pointerGates,pointerMoves,captures:files.length,zeroDelayKeyPairsVerified:false,outsidePopupPointerVerified:false,pixelParityVerified:false,images};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,images:undefined}));
