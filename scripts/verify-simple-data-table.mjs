import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
export function verifySimpleDataTable(n, source) {
 assert.equal(n.format,'orchard-simple-data-table-native-v1'); assert.equal(n.passed,true);
 assert.equal(source.format,'orchard-table-source-v1'); assert.equal(source.commit,'a86059013cd59684ce452849278ea81e734f2066');
 assert.equal(n.cases.length,26); assert.equal(n.histories.length,20); assert.equal(n.contracts.length,2); assert.equal(n.pngs.length,26);
 assert(n.guards>=600); assert.equal(n.focus,0); assert.equal(n.activations,0); assert.equal(n.publicApisOnly,true);
 for (const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted']) assert.equal(n[key],false,key);
 let geometryValues=0,maximum=0,historySnapshots=0;
 function close(a,b,label){assert(Number.isFinite(a)&&Number.isFinite(b),label);const d=Math.abs(a-b);geometryValues++;maximum=Math.max(maximum,d);assert(d<=1,label+': '+a+' versus '+b);}
 function box(a,b,label){for(const k of ['x','y','width','height'])close(a[k],b[k],label+' '+k);}
 function snapshot(a,s,label,geometry){
  for(const group of ['header','body','footer']){
   assert.equal(a[group].length,s[group].length,label+' '+group);
   for(let i=0;i<s[group].length;i++){
    const cell=a[group][i],expected=s[group][i];
    assert.equal(cell.colSpan,expected.colSpan,label+' colSpan');assert.equal(cell.rowSpan,expected.rowSpan,label+' rowSpan');
    // Source textContent retains label case; the native header retains the
    // same original label while applying uppercase only in its visual.
    if(expected.text!=='')assert.equal(cell.text,expected.text,label+' '+group+' text');
    if(geometry)box(cell.box,expected.box,label+' '+group+' '+i);
   }
  }
  assert.equal(a.rows.length,s.rows.length,label+' row count');
  for(let i=0;i<s.rows.length;i++)for(const k of ['id','index','selected'])assert.equal(a.rows[i][k]??null,s.rows[i][k]??null,label+' row '+i+' '+k);
  assert.equal(a.checkboxes.length,s.checkboxes.length,label+' checkbox count');
  for(let i=0;i<s.checkboxes.length;i++)for(const k of ['checked','indeterminate'])assert.equal(a.checkboxes[i][k],s.checkboxes[i][k],label+' checkbox '+i+' '+k);
  assert.equal(a.sortButtons.length,s.sortButtons.length);
  for(let i=0;i<s.sortButtons.length;i++){
   const actual=a.sortButtons[i],expected=s.sortButtons[i];assert.equal(actual.text,expected.text);assert.equal(actual.fontSize,parseFloat(expected.styles.fontSize));assert.equal(actual.fontWeight,Number(expected.styles.fontWeight));assert(actual.fontFamily.includes('EncodeSans-SemiBold.ttf'));
   const color=expected.styles.color.match(/^rgb\((\d+), (\d+), (\d+)\)$/);assert(color);assert.deepEqual(actual.color,[...color.slice(1).map(Number),255]);
   const icon=expected.path.includes('M17 20V4')?'PART_Unsorted':expected.path.includes('M12 19V5')?'PART_Up':'PART_Down';assert.equal(actual.icon,icon);assert.equal(Math.fround(actual.opacity),Math.fround(Number(expected.opacity)));if(geometry)box(actual.box,expected.box,label+' sort button '+i);
  }
  assert.deepEqual(a.events,s.events,label+' events');
  assert.equal(a.footerOrder,s.footerOrder,label+' footer original order');
  if(geometry){box(a.fixture,s.fixture,label+' fixture');box(a.table,{...s.table,width:s.scrollClientWidth},label+' scroll viewport');assert.equal(a.caption===null,s.caption===null);if(s.caption){assert.equal(a.caption.text,s.caption.text);box(a.caption.box,s.caption.box,label+' caption');}}
 }
 for(const scheme of ['Light','Dark']){
  const expected=source.cases.filter(c=>c.scheme===scheme&&!c.name.startsWith('primitives'));
  for(const s of expected){const matches=n.cases.filter(c=>c.scheme===scheme&&c.name===s.name);assert.equal(matches.length,1);assert.equal(matches[0].passed,true);assert.equal(matches[0].sparseGridMetadataVerified,true);snapshot(matches[0].snapshot,s.snapshot,scheme+'/'+s.name,true);}
  const histories=source.histories.filter(h=>h.scheme===scheme&&!['row-hover','original-action'].includes(h.action));
  for(const h of histories){const found=n.histories.filter(v=>v.scheme===scheme&&v.action===h.action);assert.equal(found.length,1);const a=found[0];assert.equal(a.passed,true);assert.equal(a.retainedNativeInstance,true);assert.equal(a.snapshots.length,h.snapshots.length);
   for(let i=0;i<h.snapshots.length;i++){const snap=a.snapshots[i];assert.equal(snap.stage,h.snapshots[i].stage);assert.equal(snap.nativeRequestsMatchSource,true);assert.equal(snap.originalRowsAndFooterProducerRetained,true);snapshot(snap.snapshot,h.snapshots[i],scheme+'/'+h.action+'/'+snap.stage,true);historySnapshots++;}
  }
  const contracts=n.contracts.filter(c=>c.scheme===scheme);assert.equal(contracts.length,1);for(const k of ['passed','originalNativeActionInvoked','originalCellsRetainedAfterSort','originalTableAndContentRestored','collectionChangesObserved','rowPropertyNotificationsObserved','controlledBindingsRetained','removedItemObserversReleased'])assert.equal(contracts[0][k],true,k);
 }
 assert.deepEqual(n.pngs.map(p=>p.file).sort(),n.cases.map(c=>`simple-table-${c.scheme}-${c.name}.png`).sort());
 for(const p of n.pngs){assert.match(p.sha256,/^[0-9a-f]{64}$/);assert.equal(p.width,900);assert.equal(p.height,760);}
 for(const k of ['librarySha256','hostSha256'])assert.match(n[k],/^[0-9a-f]{64}$/);
 return {passed:true,cases:26,histories:20,historySnapshots,originalContentContracts:2,geometryValues,maximumGeometryDifferenceDip:maximum,geometryToleranceDip:1,captures:26,guards:n.guards,librarySha256:n.librarySha256,hostSha256:n.hostSha256,physicalInputVerified:false,nativeComponentAccepted:false};
}
export function verifySimplePaint(actual,expected){for(let i=0;i<4;i++)assert(Math.abs(actual[i]-expected[i])<=1,'DataTable surface paint differs from its source');}
export async function auditSimpleDataTable(options){
 const n=JSON.parse(await fs.readFile(path.join(options.native,'simple-data-table.json'))),sourceBytes=await fs.readFile(options.reference),source=JSON.parse(sourceBytes);
 assert.equal(hash(sourceBytes),hash(await fs.readFile(path.join(options.native,'source-reference.json'))));
 const result=verifySimpleDataTable(n,source);
 const exit=JSON.parse(await fs.readFile(path.join(options.native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);
 for(const [file,expected]of [['Orchard.dll',n.librarySha256],[options['host-assembly']??'Orchard.Catalog.dll',n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(options.native,'app-identity',file))),expected);
 let pixels=0,paintSamples=0;
 for(const p of n.pngs){
  const bytes=await fs.readFile(path.join(options.native,p.file));assert.equal(hash(bytes),p.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,p.width);assert.equal(png.height,p.height);pixels+=png.width*png.height;
  const entry=n.cases.find(c=>`simple-table-${c.scheme}-${c.name}.png`===p.file),s=source.cases.find(c=>c.scheme===entry.scheme&&c.name===entry.name).snapshot;
  const capture=source.captures.find(c=>c.scheme===entry.scheme&&c.name===entry.name),sourcePngBytes=await fs.readFile(path.join(options.frames,capture.file));assert.equal(hash(sourcePngBytes),capture.sha256);const sourcePng=readRgbaPng(sourcePngBytes);
  for(const y of [26,Math.round(s.body[0].box.y+6),...(s.footer.length?[Math.round(s.footer[0].box.y+6)]:[])]){const at=(y*png.width+240)*4,expectedAt=(y*sourcePng.width+240)*4;verifySimplePaint([...png.rgba.subarray(at,at+4)],[...sourcePng.rgba.subarray(expectedAt,expectedAt+4)]);paintSamples++;}
 }
 result.rawPixelsValidated=pixels;result.sourcePaintSamples=paintSamples;result.sourcePaintChannelTolerance=1;result.sourceReferenceSha256=hash(sourceBytes);return result;
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const opts=Object.fromEntries(process.argv.slice(2).reduce((pairs,v,i,a)=>i%2===0?[...pairs,[v.replace(/^--/,''),a[i+1]]]:pairs,[]));
 const result=await auditSimpleDataTable(opts);await fs.writeFile(opts.output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
}
