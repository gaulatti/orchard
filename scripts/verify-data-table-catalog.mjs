import fs from 'node:fs/promises';import path from 'node:path';import crypto from 'node:crypto';import assert from 'node:assert/strict';import {fileURLToPath}from 'node:url';import {readRgbaPng}from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex'),stages=['initial','selected-first','page-two','selected-second-page','filtered','original-action','empty','cleared','count-descending','count-ascending','select-page','unselect-page'];
export function verifyDataTableCatalog(n,reference){
 assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');
 assert.equal(n.format,'orchard-data-table-catalog-native-v1');assert(n.passed);assert.equal(n.states.length,24);assert.equal(n.pngs.length,24);assert(n.guards>=180);assert.equal(n.focus,0);assert.equal(n.activations,0);assert.equal(n.publicApisOnly,true);for(const k of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(n[k],false,k);
 assert.deepEqual(n.pngs.map(p=>p.file).sort(),['Light','Dark'].flatMap(scheme=>stages.map(stage=>`data-table-catalog-${scheme}-${stage}.png`)).sort());
 for(const scheme of ['Light','Dark']){
  const states=n.states.filter(s=>s.scheme===scheme);assert.deepEqual(states.map(s=>s.stage),stages);const sort=reference.histories.find(h=>h.scheme===scheme&&h.action==='sort-cycle');const initial=reference.cases.find(c=>c.scheme===scheme&&c.name==='default').snapshot.visibleRows.map(r=>r.id),desc=sort.snapshots.find(s=>s.stage==='count-first').visibleRows.map(r=>r.id),asc=sort.snapshots.find(s=>s.stage==='count-second').visibleRows.map(r=>r.id);
  for(let i=0;i<states.length;i++){const s=states[i],empty=i===6,filtered=i===4||i===5,ids=empty?[]:filtered?['0','3','6','9','12','15','18','21']:i===2||i===3?Array.from({length:10},(_,i)=>String(i+10)):i===8?desc:i>=9?asc:initial;assert.equal(s.fixtureType,'Orchard.Catalog.DataTablesFixture');assert.equal(s.originalActionsRetained,true);assert.deepEqual(s.rows.map(r=>r.id),ids);assert.equal(s.pageIndex,i===2||i===3?1:0);assert.equal(s.pageCount,empty?0:filtered?1:3);assert.equal(s.filtered,empty?0:filtered?8:23);const selected=i===0?[]:i<3?['0']:i===10?[...new Set([...asc,'10'])].sort():i===11?['10']:['0','10'];assert.deepEqual(s.selected,selected);assert.equal(s.filteredSelected,empty?0:filtered?1:selected.length);assert.equal(s.search,filtered?'Cedar':empty?'no fictional match':'');assert.equal(s.emptyText,empty?'No results.':null);assert.equal(s.box.width,660);for(const row of s.rows){const index=Number(row.id);assert.equal(row.name,['Cedar','Birch','Ash'][index%3]+' '+(index+1));assert.equal(row.count,index*7%29);}if(i===5)assert.equal(s.status,'Original action: fictional-00');else assert.match(s.status,/^Page \d+ \/ \d+; \d+ fictional rows; \d+ selected\. Search: /);}
 }
 for(const s of n.states){
  if(s.stage!=='original-action')assert.equal(s.status,`Page ${s.pageIndex+1} / ${s.pageCount}; ${s.filtered} fictional rows; ${s.filteredSelected} selected. Search: ${s.search}`);
  for(const key of ['x','y','width','height'])assert(Number.isFinite(s.box[key]));
 }
 for(const p of n.pngs){assert.equal(p.width,900);assert.equal(p.height,1300);assert.match(p.sha256,/^[a-f0-9]{64}$/);}
 return{passed:true,actualCatalogStates:24,themes:2,originalCallerActionsVerified:true,searchSortPagingAndSelectionVerified:true,nativeComponentAccepted:false,physicalInputVerified:false,sourceRasterParityVerified:false};
}
async function main(){
 const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
 const folder=option('--native'),bytes=await fs.readFile(path.join(folder,'data-table-catalog.json')),n=JSON.parse(bytes);
 const source=await fs.readFile('tests/Reference/DataTable.json');assert.deepEqual(await fs.readFile(path.join(folder,'source-reference.json')),source);
 const audit=verifyDataTableCatalog(n,JSON.parse(source));
 assert.equal(hash(await fs.readFile(path.join(folder,'app-identity','Orchard.dll'))),n.librarySha256);
 assert.equal(hash(await fs.readFile(path.join(folder,'app-identity','Orchard.Catalog.dll'))),n.hostSha256);
 let pixels=0;for(const f of n.pngs){const b=await fs.readFile(path.join(folder,f.file));assert.equal(hash(b),f.sha256);const p=readRgbaPng(b);assert.equal(p.width,900);assert.equal(p.height,1300);pixels+=p.width*p.height;}
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert(exit.exited&&!exit.timedOut);assert.equal(exit.exitCode,0);
 Object.assign(audit,{rawPixelsValidated:pixels,librarySha256:n.librarySha256,hostSha256:n.hostSha256,sourceReferenceSha256:hash(source),reportSha256:hash(bytes)});
 await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
