import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {auditDataTablePerformance,dataTablePerformanceOperations} from './verify-data-table-performance.mjs';

export function evaluateDataTablePerformanceBudgets(reports,budgets){
 assert.equal(budgets.format,'orchard-data-table-performance-budgets-v1');
 assert.equal(budgets.independentSequentialProcesses,3);assert.equal(reports.length,3);
 assert.deepEqual(budgets.sourceRows,[128,2048,16384]);assert.equal(budgets.operationsPerProfile,20);
 assert.deepEqual(budgets.budgets.map(b=>b.pageSize),[10,100]);
 for(const b of budgets.budgets)for(const k of ['medianThreePassMilliseconds','medianManagedAllocatedBytes'])assert(Number.isFinite(b[k])&&b[k]>0);
 const first=reports[0];
 for(const report of reports){
  assert(['orchard-data-table-performance-native-v2','orchard-data-table-performance-native-v3'].includes(report.format));assert.equal(report.format,first.format);assert.equal(report.retiredRowsTrackedWeakly,true);
  if(report.format==='orchard-data-table-performance-native-v3')assert.equal(report.operationIdentityReferencesWeak,true);
  assert.equal(report.passed,true);assert.equal(report.measurements.length,120);
  for(const k of ['librarySha256','hostSha256','processorCount','runtime','os','textScaleFactor'])assert.equal(report[k],first[k]);
  assert.deepEqual(report.snapshots,first.snapshots);assert.deepEqual(report.contracts,first.contracts);
 }
 const distribution=values=>{assert.equal(values.length,3);assert(values.every(v=>Number.isFinite(v)&&v>=0));const sorted=values.toSorted((a,b)=>a-b);return{minimum:sorted[0],median:sorted[1],maximum:sorted[2],values};};
 const measurements=[];
 for(const rows of budgets.sourceRows)for(const budget of budgets.budgets)for(const operation of dataTablePerformanceOperations){
  const samples=reports.map(r=>{const matches=r.measurements.filter(m=>m.rows===rows&&m.pageSize===budget.pageSize&&m.operation===operation);assert.equal(matches.length,1,'Exactly one operation per source/page profile');return matches[0];});
  const milliseconds=distribution(samples.map(m=>m.threePassMilliseconds)),managedAllocatedBytes=distribution(samples.map(m=>m.managedAllocatedBytes));
  const timingPassed=milliseconds.median<=budget.medianThreePassMilliseconds,allocationPassed=managedAllocatedBytes.median<=budget.medianManagedAllocatedBytes;
  measurements.push({rows,pageSize:budget.pageSize,operation,milliseconds,managedAllocatedBytes,budget,timingPassed,allocationPassed,passed:timingPassed&&allocationPassed});
 }
 return{format:'orchard-data-table-performance-budget-audit-v1',passed:measurements.every(m=>m.passed),absolutePerformanceBudgetEstablished:true,independentProcesses:3,totalMeasurements:360,measurementProfiles:120,failedProfiles:measurements.filter(m=>!m.passed).length,librarySha256:first.librarySha256,hostSha256:first.hostSha256,retiredRowsTrackedWeakly:true,physicalInputVerified:false,completeMotionTimingVerified:false,nativeComponentAccepted:false,measurements};
}

if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
 const runs=option('--runs').split(',');assert.equal(runs.length,3);assert.equal(new Set(runs.map(p=>path.resolve(p))).size,3);
 const hostAssembly=process.argv.includes('--host-assembly')?option('--host-assembly'):'Orchard.Catalog.dll';
 const reports=[];for(const native of runs){await auditDataTablePerformance({native,'host-assembly':hostAssembly});reports.push(JSON.parse(await fs.readFile(path.join(native,'data-table-performance.json'))));}
 const result=evaluateDataTablePerformanceBudgets(reports,JSON.parse(await fs.readFile(option('--budgets'))));
 await fs.writeFile(option('--output'),JSON.stringify({...result,runs},null,2)+'\n');
 console.log(JSON.stringify({passed:result.passed,failedProfiles:result.failedProfiles,measurementProfiles:result.measurementProfiles,nativeComponentAccepted:false}));
 if(!result.passed)process.exitCode=1;
}
