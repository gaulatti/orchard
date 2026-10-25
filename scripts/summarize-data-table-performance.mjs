import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditDataTablePerformance,dataTablePerformanceOperations} from './verify-data-table-performance.mjs';

const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
const hostAssembly=process.argv.includes('--host-assembly')?option('--host-assembly'):'Orchard.Catalog.dll';
const folders=option('--runs').split(',');assert.equal(folders.length,3,'Use three independent sequential benchmark processes.');assert.equal(new Set(folders).size,3);
const reports=[],audits=[];
for(const native of folders){audits.push(await auditDataTablePerformance({native,'host-assembly':hostAssembly}));reports.push(JSON.parse(await fs.readFile(path.join(native,'data-table-performance.json'))));}
const first=reports[0];for(const report of reports.slice(1)){for(const key of ['format','retiredRowsTrackedWeakly','operationIdentityReferencesWeak','librarySha256','hostSha256','processorCount','runtime','os','textScaleFactor'])assert.equal(report[key],first[key]);assert.deepEqual(report.snapshots,first.snapshots);assert.deepEqual(report.contracts,first.contracts);}
const distribution=values=>{const sorted=values.toSorted((a,b)=>a-b);return{minimum:sorted[0],median:sorted[1],maximum:sorted[2],values};};
const measurements=[];
for(const rows of [128,2048,16384])for(const pageSize of [10,100])for(const operation of dataTablePerformanceOperations){const samples=reports.map(r=>r.measurements.find(m=>m.rows===rows&&m.pageSize===pageSize&&m.operation===operation));const entry={rows,pageSize,operation};for(const metric of ['mutationMilliseconds','threePassMilliseconds','cpuMilliseconds','managedAllocatedBytes','privateMemoryDeltaBytes','gcPauseMilliseconds','producerCalls','modelNotifications'])entry[metric]=distribution(samples.map(s=>s[metric]));measurements.push(entry);}
const result={passed:true,kind:'three-process-paginated-data-table-local-baseline',runs:folders,librarySha256:first.librarySha256,hostSha256:first.hostSha256,independentProcesses:3,measurementsPerRun:120,totalMeasurements:360,allSemanticSnapshotsIdentical:true,allLifecycleContractsIdentical:true,absolutePerformanceBudgetEstablished:false,performanceImprovementClaimed:false,timingIncludesMotionSettling:false,physicalInputVerified:false,nativeComponentAccepted:false,audits,measurements};
await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,audits:audits.length,measurements:measurements.length}));
