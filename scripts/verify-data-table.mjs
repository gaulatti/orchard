import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {verifyDataTableNavigationInk,dataTableNavigationInkContractSha256} from './verify-data-table-navigation-ink.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const stages=['initial','selected-first','page-two','selected-second-page','filtered','empty','restored','count-descending','count-ascending','original-action','hidden-status','restored-status','restored-template','no-columns','no-columns-no-data','columns-restored-empty','data-restored','missing-search','reloaded'];
export function verifyDataTable(n,source){
 assert.equal(n.format,'orchard-data-table-native-v1');assert.equal(n.passed,true);assert.equal(n.snapshots.length,38);assert.equal(n.pngs.length,38);assert.equal(n.contracts.length,2);assert(n.guards>=250);assert.equal(n.focus,0);assert.equal(n.activations,0);assert.equal(n.publicApisOnly,true);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','completeSourceGeometryVerified','nativeComponentAccepted'])assert.equal(n[key],false,key);
 assert.equal(source.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(source.cases.length,42);assert.equal(source.histories.length,34);
 let values=0;
 function box(b){for(const key of ['x','y','width','height'])assert(Number.isFinite(b[key]),key);assert(b.width>=0&&b.height>=0);values+=4;}
 for(const scheme of ['Light','Dark']){
  assert.deepEqual(n.snapshots.filter(s=>s.scheme===scheme).map(s=>s.stage),stages);const contract=n.contracts.find(c=>c.scheme===scheme);assert(contract);for(const k of ['passed','originalContentRetained','templateRetirementAndRestoration','unloadAndReload'])assert.equal(contract[k],true,k);
  const initial=source.cases.find(c=>c.scheme===scheme&&c.name==='default').snapshot;
  const descending=source.histories.find(h=>h.scheme===scheme&&h.action==='sort-cycle').snapshots.find(s=>s.stage==='count-first');
  const ascending=source.histories.find(h=>h.scheme===scheme&&h.action==='sort-cycle').snapshots.find(s=>s.stage==='count-second');
  const original=new Map(initial.visibleRows.map(r=>[r.id,r]));
  const all=source.histories.find(h=>h.scheme===scheme&&h.action==='paging').snapshots.flatMap(s=>s.visibleRows);for(const r of all)original.set(r.id,r);
  for(const entry of n.snapshots.filter(s=>s.scheme===scheme)){
   const s=entry.snapshot,index=stages.indexOf(entry.stage),empty=['empty','no-columns-no-data','columns-restored-empty'].includes(entry.stage),noColumns=entry.stage==='no-columns',filtered=entry.stage==='filtered';assert.equal(s.originalNativeContentVerified,true);
   const expected=empty?[]:noColumns?initial.visibleRows.map(r=>r.id):filtered?['0','3','6','9','12','15','18','21']:index>=8?ascending.visibleRows.map(r=>r.id):index===7?descending.visibleRows.map(r=>r.id):index===2||index===3?Array.from({length:10},(_,i)=>String(i+10)):initial.visibleRows.map(r=>r.id);
   assert.deepEqual(s.rows.filter(r=>r.id!==null).map(r=>r.id),expected,scheme+'/'+entry.stage+' rendered order');
   assert.deepEqual(s.selected,index===0?[]:index<3?['0']:['0','10']);assert.equal(s.filtered,empty?0:filtered?8:23);assert.equal(s.pageCount,empty?0:filtered?1:3);assert.equal(s.pageIndex,index===2||index===3?1:0);assert.equal(s.canPrevious,index===2||index===3);assert.equal(s.canNext,!empty&&!filtered);
   assert.equal(s.filteredSelected,empty?0:index===0?0:index<3||filtered?1:2);assert.equal(s.summary,s.filteredSelected?`${s.filteredSelected} of ${s.filtered} row(s) selected.`:'');assert.equal(s.search.text,filtered?'Cedar':entry.stage==='empty'?'no fictional match':'');assert.equal(s.search.box.width,384);assert.equal(s.search.box.height,40);assert.deepEqual(s.invoked,index>=9?['fictional-00']:[]);
   assert.deepEqual(s.sorting,index>=7?[{id:'Count',desc:index===7}]:[]);box(s.fixture);box(s.table);box(s.search.box);
   assert.equal(s.header.length,noColumns||entry.stage==='no-columns-no-data'?0:entry.stage==='hidden-status'?3:4);for(const h of s.header){assert.equal(h.colSpan,1);box(h.box);}
   for(const row of s.rows){box(row.box);if(empty){assert.equal(row.id,null);assert.equal(row.box.height,96);assert.equal(row.cells.length,1);assert.equal(row.cells[0].content,'No results.');assert.equal(row.cells[0].colSpan,entry.stage==='no-columns-no-data'?1:4);}else if(noColumns){assert.equal(row.cells.length,0);assert.equal(row.box.height,0);}else{const r=original.get(row.id);assert(r);const columns=entry.stage==='hidden-status'?['select','Name','Count']:['select','Name','Count','Status'];assert.deepEqual(row.cells.map(c=>c.column),columns);for(const cell of row.cells){assert.equal(cell.colSpan,1);const value=cell.column==='select'?s.selected.includes(row.id):r.values.find(v=>v.column===cell.column.toLowerCase()).value;assert.equal(cell.content,cell.column === 'select' ? value : String(value),scheme+'/'+entry.stage+'/'+row.id+'/'+cell.column);}}for(const cell of row.cells)box(cell.box);}
  }
 }
 return {passed:true,nativeStates:38,themes:2,contracts:2,geometryValuesInspected:values,frozenSourceValuesAndRenderedOrderVerified:true,originalNativeContentVerified:true,completeSourceHistoryReplay:false,completeSourceGeometryVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
export function verifyDataTablePaint(n,p,image){
 const scheme=p.file.includes('-Dark-')?'Dark':'Light',stage=p.file.slice(`data-table-${scheme}-`.length,-4),entry=n.snapshots.find(s=>s.scheme===scheme&&s.stage===stage);assert(entry,'Capture has no measured native state');
 const s=entry.snapshot,fixture=s.fixture,cy=Math.round(fixture.y+fixture.height-16);
 return verifyDataTableNavigationInk({scheme,stage,buttons:[{name:'previous',cx:Math.round(fixture.x+fixture.width-64),cy,enabled:s.canPrevious},{name:'next',cx:Math.round(fixture.x+fixture.width-24),cy,enabled:s.canNext}],image});
}
async function main(){
 const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};const folder=option('--native'),bytes=await fs.readFile(path.join(folder,'data-table.json')),n=JSON.parse(bytes),sourceBytes=await fs.readFile('tests/Reference/DataTable.json'),audit=verifyDataTable(n,JSON.parse(sourceBytes));
 assert.equal(hash(await fs.readFile(path.join(folder,'app-identity','Orchard.dll'))),n.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',option('--host-assembly')))),n.hostSha256);
 let pixels=0,arrowPaintProbes=0;for(const p of n.pngs){const bytes=await fs.readFile(path.join(folder,p.file));assert.equal(hash(bytes),p.sha256);const image=readRgbaPng(bytes);assert.equal(image.width,900);assert.equal(image.height,1000);assert.equal(p.width,900);assert.equal(p.height,1000);pixels+=image.width*image.height;arrowPaintProbes+=verifyDataTablePaint(n,p,image);}
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);Object.assign(audit,{rawPixelsValidated:pixels,arrowPaintProbes,navigationInkContractSha256:dataTableNavigationInkContractSha256,librarySha256:n.librarySha256,hostSha256:n.hostSha256,sourceReferenceSha256:hash(sourceBytes),reportSha256:hash(bytes)});await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
