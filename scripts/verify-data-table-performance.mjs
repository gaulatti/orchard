import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const digest=values=>hash(values.join('\n'));
export const dataTablePerformanceOperations=['bind-data','sort-descending','sort-ascending','next-page','select-page','deselect-page','select-all','deselect-all','notify-hidden','notify-visible','range-filter','clear-range','search-group','clear-search','global-group','clear-global','reverse-source','clear-sort','remove-first','append-original'];
export function verifyDataTablePerformance(n,{diagnostic=false,pagingOnly=false}={}){
 if(n.containerContracts!==undefined){
  assert.deepEqual(n.containerContracts.map(row=>row.mode).sort(),['cell-context','cell-template','default','row-context','row-style']);
  for(const row of n.containerContracts){
   for(const field of ['passed','originalDataRetained','retiredContentUnloaded','returnedContentCorrect'])assert.equal(row[field],true,row.mode+'/'+field);
   if(row.mode==='default')assert([0,2].includes(row.reusedRowContainers));else assert.equal(row.reusedRowContainers,0,row.mode+'/caller lifecycle');
  }
 }
 if(n.performanceScope!==undefined)assert(['full','paging-16384-100'].includes(n.performanceScope),'Known performance scope required');
 if(diagnostic){assert.equal(n.format,'orchard-data-table-performance-diagnostic-v1');assert.equal(n.diagnosticEvents,true);assert.equal(n.normalTimingRun,false);assert(Number.isInteger(n.phaseMarkers)&&n.phaseMarkers>=2000);}
 else{assert(['orchard-data-table-performance-native-v1','orchard-data-table-performance-native-v2','orchard-data-table-performance-native-v3'].includes(n.format),'Known native benchmark format required');assert.notEqual(n.diagnosticEvents,true,'Diagnostic traces cannot qualify as performance runs');assert.notEqual(n.normalTimingRun,false);}
 if(n.format!=='orchard-data-table-performance-native-v1')assert.equal(n.retiredRowsTrackedWeakly,true,'Retired rows must not be retained by benchmark instrumentation');
 if(diagnostic||n.format==='orchard-data-table-performance-native-v3')assert.equal(n.operationIdentityReferencesWeak,true,'Operation identity checks must not retain an outgoing native page');
 const counts=pagingOnly?[16384]:[128,2048,16384],sizes=pagingOnly?[100]:[10,100],profiles=counts.length*sizes.length;if(pagingOnly)assert.equal(n.performanceScope,'paging-16384-100');else assert.notEqual(n.performanceScope,'paging-16384-100','Focused paging cannot qualify as the full matrix');
 assert.equal(n.passed,true);assert.equal(n.publicApisOnly,true);assert.equal(n.measurements.length,20*profiles);assert.equal(n.snapshots.length,20*profiles);assert.equal(n.contracts.length,profiles);assert.equal(n.pngs.length,pagingOnly?0:6);assert(n.guards>=(pagingOnly?200:700));assert.equal(n.focus,0);assert.equal(n.activations,0);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted','forcedGc','timingIncludesMotionSettling'])assert.equal(n[key],false,key);
 for(const key of ['timingIncludesThreeDispatcherIdles','captureAndVerificationExcludedFromTimings','controlConstructionExcludedFromTimings','replacementDataPreparationExcludedFromTimings','capturesPageSize10Only','unloadObservationOutsideTimings','lateLoadedHandlersAvoided'])assert.equal(n[key],true,key);
 assert.deepEqual(n.loadingDiagnostics.map(d=>d.timing),['none','before-attach','after-layout','unloaded-after-layout']);for(const d of n.loadingDiagnostics){for(const k of ['initial','afterDelay'])assert.equal(typeof d[k],'boolean');for(const k of ['rowHasXamlRoot','parentLoaded','cellLoaded','tableLoaded','rootLoaded'])assert.equal(d[k],true,k);assert.equal(d.gridRows,1);assert.equal(d.gridColumns,1);assert(Number.isInteger(d.loadedEvents)&&d.loadedEvents>=0);}assert.equal(n.loadingDiagnostics[1].afterDelay,true);assert.equal(n.loadingDiagnostics[3].afterDelay,true);
 assert.equal(n.warmupRows,16);assert(Number.isInteger(n.processorCount)&&n.processorCount>0);assert(Number.isFinite(n.textScaleFactor)&&n.textScaleFactor>=1);assert.equal(typeof n.runtime,'string');assert.equal(typeof n.os,'string');
 let renderedRows=0,retainedRows=0;
 for(const count of counts)for(const size of sizes){
  let source=Array.from({length:count},(_,i)=>i),descending=null,page=0,range=false,nameFilter=false,global=false,removed;const selected=new Set(),names=new Map();let previousIds=[];
  const originalId=i=>'fictional-'+String(i).padStart(5,'0');
  const baseName=i=>'Group '+i%8+' item '+String(i).padStart(5,'0');
  const name=i=>names.get(i)??baseName(i);
  const filtered=()=>source.filter(i=>(!range||(count-i>=count/4&&count-i<=count/2))&&((!nameFilter&&!global)||name(i).toLowerCase().includes('group 3')));
  const sorted=()=>{const rows=filtered();return descending===null?rows:rows.sort((a,b)=>descending?a-b:b-a);};
  for(const operation of dataTablePerformanceOperations){
   switch(operation){
    case'bind-data':break;
    case'sort-descending':descending=true;page=0;break;
    case'sort-ascending':descending=false;page=0;break;
    case'next-page':page=1;break;
    case'select-page':for(const i of sorted().slice(page*size,(page+1)*size))selected.add(String(source.indexOf(i)));break;
    case'deselect-page':selected.clear();break;
    case'select-all':for(let i=0;i<source.length;i++)selected.add(String(i));break;
    case'deselect-all':selected.clear();break;
    case'notify-hidden':{const visible=new Set(sorted().slice(page*size,(page+1)*size)),i=source.find(i=>!visible.has(i));assert.notEqual(i,undefined);names.set(i,baseName(i)+' hidden-update');page=0;break;}
    case'notify-visible':{const i=sorted()[0];names.set(i,baseName(i)+' visible-update');page=0;break;}
    case'range-filter':range=true;page=0;break;
    case'clear-range':range=false;page=0;break;
    case'search-group':nameFilter=true;page=0;break;
    case'clear-search':nameFilter=false;page=0;break;
    case'global-group':global=true;page=0;break;
    case'clear-global':global=false;page=0;break;
    case'reverse-source':source.reverse();page=0;break;
    case'clear-sort':descending=null;page=0;break;
    case'remove-first':removed=source.shift();page=0;break;
    case'append-original':source.push(removed);page=0;break;
    default:assert.fail(operation);
   }
   const matches=rows=>rows.filter(s=>s.rows===count&&s.pageSize===size&&s.operation===operation),ms=matches(n.measurements),states=matches(n.snapshots);assert.equal(ms.length,1);assert.equal(states.length,1);const m=ms[0],s=states[0].snapshot,position=dataTablePerformanceOperations.indexOf(operation);
   for(const key of ['mutationMilliseconds','threePassMilliseconds','cpuMilliseconds','managedAllocatedBytes','gcPauseMilliseconds'])assert(Number.isFinite(m[key])&&m[key]>=0,key);assert(m.threePassMilliseconds>=m.mutationMilliseconds);assert(Number.isInteger(m.privateMemoryDeltaBytes));assert.equal(m.gcCollections.length,3);assert(m.gcCollections.every(i=>Number.isInteger(i)&&i>=0));assert(Number.isInteger(m.producerCalls)&&m.producerCalls>=0);assert(Number.isInteger(m.modelNotifications)&&m.modelNotifications>=1);
   const filteredRows=filtered(),ordered=sorted(),visible=ordered.slice(page*size,(page+1)*size),indices=new Map(source.map((i,index)=>[i,index])),ids=visible.map(i=>String(indices.get(i)));
   assert.equal(s.operation,operation);assert.equal(s.sourceCount,source.length);assert.equal(s.coreRows,source.length);assert.equal(s.filteredRows,filteredRows.length);assert.equal(s.filteredSelectedRows,filteredRows.filter(i=>selected.has(String(indices.get(i)))).length);assert.equal(s.pageIndex,page);assert.equal(s.pageSize,size);assert.equal(s.pageCount,Math.ceil(filteredRows.length/size));assert.equal(s.selectedCount,selected.size);
   assert.equal(s.selectedIdsSha256,digest([...selected].sort()));assert.equal(s.sourceOriginalIdsSha256,digest(source.map(originalId)));assert.equal(s.filteredOriginalIdsSha256,digest(filteredRows.map(originalId)));assert.equal(s.sortedOriginalIdsSha256,digest(ordered.map(originalId)));assert.equal(s.rows.length,visible.length);assert(s.rows.length<=size);
   s.rows.forEach((r,index)=>{const i=visible[index];assert.equal(r.rowId,ids[index]);assert.equal(r.originalId,originalId(i));assert.equal(r.name,name(i));assert.equal(r.count,String(count-i));assert.equal(r.selected,selected.has(ids[index]));assert.equal(r.originalActionRetained,true);assert.equal(r.loaded,true,`${count}/${size}/${operation}: native row loaded`);assert(Number.isFinite(r.width)&&r.width>0);assert(Number.isFinite(r.height)&&r.height>0);renderedRows++;});
   const before=new Set(previousIds),retained=ids.filter(id=>before.has(id)).length;assert.equal(s.retainedNativeRows,retained);assert.equal(s.retainedNativeRowUnloads,0);retainedRows+=retained;previousIds=ids;
   assert.equal(s.itemSubscriptions,source.length);assert.equal(s.collectionSubscriptions,1);assert.equal(s.wrongItemSubscriptions,0);assert.equal(s.wrongCollectionSubscriptions,0);assert.equal(s.sortInvocations,position===0?0:position===1?1:2);assert.equal(s.selectionInvocations,position<4?0:position===4?1:2);
  }
  const contracts=n.contracts.filter(c=>c.rows===count&&c.pageSize===size);assert.equal(contracts.length,1);const contract=contracts[0];assert.equal(contract.unloadedItemSubscriptions,0);assert.equal(contract.unloadedCollectionSubscriptions,0);assert.equal(contract.unloadedNotificationsIgnored,true);assert.equal(contract.originalActionId,originalId(source[0]));
 }
 assert.deepEqual(n.pngs.map(p=>p.file).sort(),(pagingOnly?[]:[128,2048,16384]).flatMap(n=>['bind-data','append-original'].map(op=>`data-table-performance-${n}-10-${op}.png`)).sort());for(const p of n.pngs){assert.equal(p.width,1000);assert.equal(p.height,1000);assert.match(p.sha256,/^[0-9a-f]{64}$/);}for(const k of ['librarySha256','hostSha256'])assert.match(n[k],/^[0-9a-f]{64}$/);
 return{passed:true,component:'BleeckerDataTable',sourceRowCounts:counts,pageSizes:sizes,verificationScope:pagingOnly?"focused-paging":"full",fullPerformanceMatrixVerified:!pagingOnly,operationsPerProfile:20,measurements:20*profiles,renderedRows,retainedNativeRows:retainedRows,paginatedNativeRowsBounded:true,originalDataControlsAndNotificationsVerified:true,retainedRowsRemainLoaded:true,unloadedProducerSubscriptionsReleased:true,absolutePerformanceBudgetEstablished:false,completeMotionTimingVerified:false,physicalInputVerified:false,nativeComponentAccepted:false,librarySha256:n.librarySha256,hostSha256:n.hostSha256};
}
export function verifyDataTablePerformanceDiagnostic(n){return{...verifyDataTablePerformance(n,{diagnostic:true}),diagnosticProfile:true,normalTimingRun:false};}
export async function auditDataTablePerformance(options,{diagnostic=false}={}){if(options.scope!==undefined)assert(["full","paging"].includes(options.scope));const pagingOnly=options.scope==="paging";assert(!diagnostic||!pagingOnly);const bytes=await fs.readFile(path.join(options.native,'data-table-performance.json')),n=JSON.parse(bytes),audit=diagnostic?verifyDataTablePerformanceDiagnostic(n):verifyDataTablePerformance(n,{pagingOnly}),exit=JSON.parse(await fs.readFile(path.join(options.native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);for(const[file,sha]of [['Orchard.dll',n.librarySha256],[options['host-assembly']??'Orchard.Catalog.dll',n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(options.native,'app-identity',file))),sha);let pixels=0;for(const entry of n.pngs){const bytes=await fs.readFile(path.join(options.native,entry.file));assert.equal(hash(bytes),entry.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,entry.width);assert.equal(png.height,entry.height);pixels+=png.width*png.height;}return{...audit,rawPixelsValidated:pixels,reportSha256:hash(bytes)};}
export async function auditDataTablePerformanceDiagnostic(options){return auditDataTablePerformance(options,{diagnostic:true});}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){const args=process.argv.slice(2);assert.equal(args.length%2,0);const options=Object.fromEntries(args.reduce((pairs,value,i)=>i%2===0?[...pairs,[value.replace(/^--/,''),args[i+1]]]:pairs,[]));const audit=await auditDataTablePerformance(options);await fs.writeFile(options.output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));}
