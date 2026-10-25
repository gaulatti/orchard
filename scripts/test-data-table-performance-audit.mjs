import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {verifyDataTablePerformance} from './verify-data-table-performance.mjs';

const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
const native=JSON.parse(await fs.readFile(path.join(option('--native'),'data-table-performance.json')));
verifyDataTablePerformance(native);
const focused={...native,performanceScope:'paging-16384-100',measurements:native.measurements.filter(s=>s.rows===16384&&s.pageSize===100),snapshots:native.snapshots.filter(s=>s.rows===16384&&s.pageSize===100),contracts:native.contracts.filter(s=>s.rows===16384&&s.pageSize===100),pngs:[]};
assert.equal(verifyDataTablePerformance(focused,{pagingOnly:true}).fullPerformanceMatrixVerified,false);
assert.throws(()=>verifyDataTablePerformance(focused),/Focused paging/);
const focusedMutations=[['missing-measurement',n=>n.measurements.pop()],['wrong-row',n=>n.snapshots[0].snapshot.rows[0].name='forged'],['replaced-action',n=>n.snapshots[0].snapshot.rows[0].originalActionRetained=false],['retained-unload',n=>n.snapshots[0].snapshot.retainedNativeRowUnloads=1],['producer-leak',n=>n.contracts[0].unloadedItemSubscriptions=1],['wrong-scope',n=>n.performanceScope='full']];
for(const[name,mutate]of focusedMutations){const bad=structuredClone(focused);mutate(bad);assert.throws(()=>verifyDataTablePerformance(bad,{pagingOnly:true}),undefined,name);}
const state=n=>n.snapshots[0].snapshot;
const mutations=[
 ...(native.containerContracts ? [
  ['missing-container-mode',n=>n.containerContracts.pop()],
  ['implicit-style-container-reused',n=>n.containerContracts.find(row=>row.mode==='row-style').reusedRowContainers=2],
  ['caller-editor-state-lost',n=>n.containerContracts.find(row=>row.mode==='default').returnedContentCorrect=false],
  ['retired-caller-editor-loaded',n=>n.containerContracts.find(row=>row.mode==='cell-template').retiredContentUnloaded=false]
 ] : []),
 ['unknown-format',n=>n.format='orchard-data-table-performance-native-unknown'],
 ...(native.format!=='orchard-data-table-performance-native-v1' ? [['strong-retired-row-tracker',n=>n.retiredRowsTrackedWeakly=false],['missing-tracker-contract',n=>delete n.retiredRowsTrackedWeakly]] : []),
 ...(native.format==='orchard-data-table-performance-native-v3' ? [['strong-operation-identities',n=>n.operationIdentityReferencesWeak=false],['missing-operation-identity-contract',n=>delete n.operationIdentityReferencesWeak]] : []),
 ...['passed','publicApisOnly','timingIncludesThreeDispatcherIdles','captureAndVerificationExcludedFromTimings','controlConstructionExcludedFromTimings','replacementDataPreparationExcludedFromTimings','capturesPageSize10Only','unloadObservationOutsideTimings','lateLoadedHandlersAvoided'].map(k=>[k,n=>n[k]=false]),
 ...['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted','forcedGc','timingIncludesMotionSettling'].map(k=>[k,n=>n[k]=true]),
 ['focus',n=>n.focus=1],['activation',n=>n.activations=1],['missing-guards',n=>n.guards=0],
 ['missing-loading-diagnostic',n=>n.loadingDiagnostics.pop()],['grid-diagnostic',n=>n.loadingDiagnostics[0].gridRows=0],['unloaded-observation',n=>n.loadingDiagnostics[3].afterDelay=false],
 ['missing-measurement',n=>n.measurements.pop()],['duplicate-measurement',n=>n.measurements[1]=structuredClone(n.measurements[0])],['missing-state',n=>n.snapshots.pop()],['missing-contract',n=>n.contracts.pop()],['missing-frame',n=>n.pngs.pop()],
 ['bad-mutation-time',n=>n.measurements[0].mutationMilliseconds=-1],['bad-layout-time',n=>n.measurements[0].threePassMilliseconds=0],['bad-cpu-time',n=>n.measurements[0].cpuMilliseconds=Infinity],['negative-allocation',n=>n.measurements[0].managedAllocatedBytes=-1],['bad-gc',n=>n.measurements[0].gcCollections=[0,-1,0]],['negative-producer-calls',n=>n.measurements[0].producerCalls=-1],['missing-model-notification',n=>n.measurements[0].modelNotifications=0],
 ['original-id',n=>state(n).rows[0].originalId='replacement'],['index-id',n=>state(n).rows[0].rowId='1'],['rendered-name',n=>state(n).rows[0].name='stale'],['rendered-count',n=>state(n).rows[0].count='0'],['selection',n=>state(n).rows[0].selected=true],['action',n=>state(n).rows[0].originalActionRetained=false],['unloaded-row',n=>state(n).rows[0].loaded=false],['collapsed-row',n=>state(n).rows[0].height=0],
 ['core-count',n=>state(n).coreRows--],['filtered-count',n=>state(n).filteredRows--],['filtered-selection',n=>state(n).filteredSelectedRows=1],['page-index',n=>state(n).pageIndex=1],['page-size',n=>state(n).pageSize=100],['page-count',n=>state(n).pageCount=0],['total-selection',n=>state(n).selectedCount=1],
 ...['sourceOriginalIdsSha256','filteredOriginalIdsSha256','sortedOriginalIdsSha256','selectedIdsSha256'].map(k=>[k,n=>state(n)[k]='0'.repeat(64)]),
 ['retained-count',n=>state(n).retainedNativeRows=1],['retained-unload',n=>state(n).retainedNativeRowUnloads=1],['item-subscriptions',n=>state(n).itemSubscriptions=0],['collection-subscriptions',n=>state(n).collectionSubscriptions=0],['wrong-item-subscriptions',n=>state(n).wrongItemSubscriptions=1],['wrong-collection-subscriptions',n=>state(n).wrongCollectionSubscriptions=1],['missing-sort-action',n=>n.snapshots[1].snapshot.sortInvocations=0],['missing-selection-action',n=>n.snapshots[4].snapshot.selectionInvocations=0],
 ['unload-item-leak',n=>n.contracts[0].unloadedItemSubscriptions=1],['unload-collection-leak',n=>n.contracts[0].unloadedCollectionSubscriptions=1],['unloaded-notification',n=>n.contracts[0].unloadedNotificationsIgnored=false],['original-action-id',n=>n.contracts[0].originalActionId='replacement'],['duplicate-frame',n=>n.pngs[1].file=n.pngs[0].file],['wrong-capture-size',n=>n.pngs[0].width=999]
];
for(const[name,mutate]of mutations){const bad=structuredClone(native);mutate(bad);assert.throws(()=>verifyDataTablePerformance(bad),undefined,name);}
const retained=JSON.parse(await fs.readFile('tests/Reference/DataTablePerformanceLateObservation.json'));assert.equal(retained.format,'orchard-data-table-performance-late-observation-v1');assert.equal(retained.componentDefectClaimed,false);const bytes=await fs.readFile(retained.frame);assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),retained.frameSha256);const png=readRgbaPng(bytes);assert.equal(png.width,1000);assert.equal(png.height,1000);const bad=structuredClone(native);bad.snapshots[0].snapshot=retained.scene.snapshot;assert.throws(()=>verifyDataTablePerformance(bad),/native row loaded/);
const audit={passed:true,corruptReportsRejected:mutations.length,corruptFocusedReportsRejected:focusedMutations.length,focusedReportRejectedAsFull:true,actualLateObservationStatesRejected:1,actualOriginalCapturePreserved:true,absolutePerformanceBudgetEstablished:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
