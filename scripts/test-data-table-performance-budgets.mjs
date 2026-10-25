import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditDataTablePerformance} from './verify-data-table-performance.mjs';
import {evaluateDataTablePerformanceBudgets} from './verify-data-table-performance-budgets.mjs';
const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
const native=option('--native'),hostAssembly=process.argv.includes('--host-assembly')?option('--host-assembly'):'Orchard.Catalog.dll';await auditDataTablePerformance({native,'host-assembly':hostAssembly});
const actual=JSON.parse(await fs.readFile(path.join(native,'data-table-performance.json'))),budgets=JSON.parse(await fs.readFile(option('--budgets')));
const atBoundary=Array.from({length:3},()=>structuredClone(actual));
for(const report of atBoundary)for(const m of report.measurements){const b=budgets.budgets.find(b=>b.pageSize===m.pageSize);m.threePassMilliseconds=b.medianThreePassMilliseconds;m.managedAllocatedBytes=b.medianManagedAllocatedBytes;}
assert.equal(evaluateDataTablePerformanceBudgets(atBoundary,budgets).passed,true,'Inclusive budget boundary');
let rejected=0;
for(const metric of ['threePassMilliseconds','managedAllocatedBytes']){
 const reports=structuredClone(atBoundary);for(const r of reports.slice(0,2))r.measurements[0][metric]+=1;
 const result=evaluateDataTablePerformanceBudgets(reports,budgets);assert.equal(result.passed,false);assert.equal(result.failedProfiles,1);rejected++;
 const oneOutlier=structuredClone(atBoundary);oneOutlier[0].measurements[0][metric]+=10000;assert.equal(evaluateDataTablePerformanceBudgets(oneOutlier,budgets).passed,true,'One outlier cannot replace the three-process median');
}
const mutations=[
 ['v1-tracker',r=>r[0].format='orchard-data-table-performance-native-v1'],
 ['strong-tracker',r=>r[0].retiredRowsTrackedWeakly=false],
 ...(actual.format==='orchard-data-table-performance-native-v3' ? [['strong-operation-identities',r=>r[0].operationIdentityReferencesWeak=false],['mixed-v2-operation-map',r=>r[1].format='orchard-data-table-performance-native-v2']] : []),
 ['mixed-library',r=>r[1].librarySha256='0'.repeat(64)],
 ['mixed-host',r=>r[1].hostSha256='0'.repeat(64)],
 ['mixed-runtime',r=>r[1].runtime='other'],
 ['missing-operation',r=>r[0].measurements.pop()],
 ['duplicate-operation',r=>r[0].measurements[1]=structuredClone(r[0].measurements[0])],
 ['negative-time',r=>r[0].measurements[0].threePassMilliseconds=-1],
 ['invalid-allocation',r=>r[0].measurements[0].managedAllocatedBytes=Infinity],
 ['changed-visible-row',r=>r[1].snapshots[0].snapshot.rows[0].name='different'],
 ['changed-lifecycle',r=>r[1].contracts[0].unloadedItemSubscriptions=1]
];
for(const[name,mutate]of mutations){const r=structuredClone(atBoundary);mutate(r);assert.throws(()=>evaluateDataTablePerformanceBudgets(r,budgets),undefined,name);rejected++;}
assert.throws(()=>evaluateDataTablePerformanceBudgets(atBoundary.slice(1),budgets));rejected++;
const result={passed:true,syntheticCounterexamplesRejected:rejected,inclusiveTimingAndAllocationBoundariesVerified:true,threeProcessMedianVerified:true,realNativeInputAudited:true,syntheticResultsArePerformanceEvidence:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
