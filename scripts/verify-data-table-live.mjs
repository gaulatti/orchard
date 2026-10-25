import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export const liveStages=['initial','equal-object-notified','collection-add','collection-move','collection-replace','retired-item-notified','collection-remove','collection-clear','collection-restored','source-replaced','reloaded','template-restored','original-action','column-styles','local-binding','notified-binding','column-styles-cleared','local-binding-cleared','duplicate-reference','duplicate-reference-notified'];
function expected(index){
 const name={a:'Twin',b:'Twin',c:'Cedar',d:'Birch',u:'Orchard',v:'Grove',w:'Meadow',x:'Willow',y:'Field'};
 const ids=index===0||index===1?['a','b']:index===2?['a','b','c']:index===3?['c','a','b']:index===4||index===5?['c','d','b']:index===6?['c','d']:index===7?[]:index===8?['a','b']:index===9?['u','v']:index===10?['u','v','w']:index<18?['u','v','w','x']:index<21?['u','v','w','x','v']:['u','v','w','x','v','y'];
 const counts={a:index>=8?99:1,b:index===0?1:42,c:3,d:4,u:5,v:index===9?6:index===10?77:index<19?88:index===19?111:112,w:7,x:8,y:9};
 return ids.map(id=>({id,name:name[id],count:counts[id]}));
}
export function verifyDataTableLive(n){
 assert.equal(n.format,'orchard-data-table-live-native-v1');assert.equal(n.passed,true);assert.deepEqual(n.kinds,['data-table','simple']);assert.equal(n.states.length,84);assert.equal(n.pngs.length,84);assert.equal(n.contracts.length,4);assert(n.guards>=700);assert.equal(n.focus,0);assert.equal(n.activations,0);assert.equal(n.publicApisOnly,true);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(n[key],false,key);
 const frames=[];let rows=0,subscriptions=0;
 for(const scheme of ['Light','Dark'])for(const kind of ['data-table','simple']){
  const stages=[...liveStages,...(kind==='data-table'?['worker-item-notified','worker-collection-add']:[])],states=n.states.filter(s=>s.scheme===scheme&&s.kind===kind);assert.deepEqual(states.map(s=>s.snapshot.stage),stages);
  const contract=n.contracts.find(c=>c.scheme===scheme&&c.kind===kind);assert(contract);for(const key of ['distinctEqualOriginalsVerified','repeatedOriginalObservedOnce','replacedProducersReleased','unloadedProducersReleased','templateContentRetired','originalCellAndBindingRetained'])assert.equal(contract[key],true,key);assert.equal(contract.workerNotificationsVerified,kind==='data-table');
  states.forEach((entry,index)=>{
   const s=entry.snapshot,want=expected(index),stage=stages[index],file=`data-table-live-${scheme}-${kind}-${stage}.png`;assert.equal(entry.file,file);frames.push(file);assert.equal(s.loaded,true);assert.deepEqual(s.expectedRows,want);assert.equal(s.rows.length,want.length);assert.equal(s.emptyText,want.length?null:'No results.');assert.equal(s.box.width,660);
   assert.deepEqual(s.invoked,index>=12?['u']:[]);
   s.rows.forEach((r,i)=>{const e=want[i];assert.equal(r.id,e.id);assert.equal(r.originalRetained,true);assert.equal(r.originalActionRetained,true);assert.equal(r.name,e.name);assert.equal(r.count,String(e.count));const size=index===13?20:index===14?i===0?24:20:index===15?i===0?28:20:index===16&&i===0?28:14;assert.equal(r.nameFont,size);assert.equal(r.renderedNameFont,size);assert.equal(r.headerFont,index>=13&&index<=15?18:11);assert.equal(r.alignment,index>=13&&index<=15?'Right':'Left');for(const k of ['x','y','width','height'])assert(Number.isFinite(r.box[k]));assert(r.box.width>0&&r.box.height>0);rows++;});
   const known=index<2?['a','b']:index<4?['a','b','c']:index<9?['a','b','c','d']:index<10?['a','b','c','d','u','v']:index<11?['a','b','c','d','u','v','w']:index<21?['a','b','c','d','u','v','w','x']:['a','b','c','d','u','v','w','x','y'];assert.deepEqual(s.subscriptions.map(i=>i.id),known);
   for(const i of s.subscriptions){const count=want.some(e=>e.id===i.id)?1:0;assert.equal(i.count,count,i.id+' live subscription');assert.equal(i.expected,count);subscriptions++;}
   assert.equal(s.collectionSubscriptions.length,index<9?1:2);s.collectionSubscriptions.forEach((c,i)=>{const count=index<9?1:i===1?1:0;assert.equal(c.count,count);assert.equal(c.expected,count);subscriptions++;});
  });
 }
 assert.deepEqual(n.pngs.map(p=>p.file).sort(),frames.sort());for(const p of n.pngs){assert.equal(p.width,900);assert.equal(p.height,1000);assert.match(p.sha256,/^[a-f0-9]{64}$/);}for(const key of ['librarySha256','hostSha256'])assert.match(n[key],/^[a-f0-9]{64}$/);
 return{passed:true,states:84,themes:2,compositions:2,renderedRows:rows,subscriptionCountsVerified:subscriptions,equalOriginalNotificationsVerified:true,repeatedOriginalObservedOnce:true,lifecycleAndStyleBindingsVerified:true,dataTableWorkerNotificationsVerified:true,physicalInputVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
async function main(){
 const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};const folder=option('--native'),bytes=await fs.readFile(path.join(folder,'data-table-live.json')),n=JSON.parse(bytes),audit=verifyDataTableLive(n);
 assert.equal(hash(await fs.readFile(path.join(folder,'app-identity','Orchard.dll'))),n.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',option('--host-assembly')))),n.hostSha256);
 let pixels=0;for(const f of n.pngs){const bytes=await fs.readFile(path.join(folder,f.file));assert.equal(hash(bytes),f.sha256);const image=readRgbaPng(bytes);assert.equal(image.width,900);assert.equal(image.height,1000);pixels+=image.width*image.height;}
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,0);Object.assign(audit,{rawPixelsValidated:pixels,librarySha256:n.librarySha256,hostSha256:n.hostSha256,reportSha256:hash(bytes)});await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
