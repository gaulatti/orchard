import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createDataTableCostFixture} from './create-data-table-cost-fixture.mjs';
import {verifyDataTablePerformance,verifyDataTablePerformanceDiagnostic} from './verify-data-table-performance.mjs';
const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
const source=await fs.readFile('tests/PackageConsumer/DataTablePerformanceFixture.cs','utf8'),generated=createDataTableCostFixture(source);
assert(generated.sourceRoundtripExact);assert.notEqual(generated.sourceSha256,generated.generatedSha256);assert(!source.includes('Orchard-DataTable-NativeCost'));assert(generated.source.includes('Orchard-DataTable-NativeCost'));
assert.throws(()=>createDataTableCostFixture(source.replaceAll('orchard-data-table-performance-native-v3','future-format')));
const diagnostic=JSON.parse(await fs.readFile(option('--native')+'/data-table-performance.json')),normal=JSON.parse(await fs.readFile(option('--baseline')+'/data-table-performance.json'));
verifyDataTablePerformanceDiagnostic(diagnostic);verifyDataTablePerformance(normal);
assert.throws(()=>verifyDataTablePerformance(diagnostic));assert.throws(()=>verifyDataTablePerformanceDiagnostic(normal));
const mutations=[n=>n.diagnosticEvents=false,n=>n.normalTimingRun=true,n=>n.phaseMarkers=0,n=>n.operationIdentityReferencesWeak=false,n=>n.retiredRowsTrackedWeakly=false,n=>n.physicalInputVerified=true,n=>n.nativeComponentAccepted=true,n=>n.measurements.pop(),n=>n.snapshots[0].snapshot.rows[0].originalId='wrong-original',n=>n.contracts[0].unloadedItemSubscriptions=1];
for(const change of mutations){const n=structuredClone(diagnostic);change(n);assert.throws(()=>verifyDataTablePerformanceDiagnostic(n));}
const result={passed:true,normalGateRejectsActualDiagnostic:true,diagnosticGateRejectsActualNormal:true,sourceTransformationRoundtripExact:true,changedSourceAnchorRejected:true,corruptDiagnosticReportsRejected:mutations.length,normalTimingRun:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
