import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const monitoring=['loaded','narrow-viewport','native-selection','native-search','retained-update','inspector','sidebar-attention','review-snapshot','discard-decision','keep-review','local-review','explicit-review-discard','review-history','suite-request'];
const editorial=['loaded','narrow-viewport','native-filters','sort-retention','search-empty','native-edit','relations-order','visible-choice-order','row-discard-decision','keep-exact-draft','catalog-remount','local-save-original','explicit-row-discard','save-validation','suite-discard-decision','section-discard-decision','section-discard','suite-discard','monitoring-remount'];
export function auditWorkspaceReport(report){
 assert.equal(report.scope,'hidden-workspace-demo-contracts');
 for(const key of ['passed','hiddenWindow','isolated'])assert.equal(report[key],true,key);
 for(const key of ['activateCalled','showCalled','focusCalled','inputSent','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[key],false,key);
 assert.equal(report.activationEvents,0);assert.equal(report.failure,null);assert.equal(report.isolationFailure,null);assert.deepEqual(report.resourceErrors,[]);assert.deepEqual(report.bindingErrors,[]);
 assert.equal(report.processArchitecture,report.osArchitecture);assert(['X64','Arm64'].includes(report.processArchitecture));
 const expected=['Light','Dark'].flatMap(theme=>[...monitoring.map(id=>`${theme}/monitoring/${id}`),...editorial.map(id=>`${theme}/editorial/${id}`)]).sort();
 assert.deepEqual(report.checks.map(check=>check.id).sort(),expected);for(const check of report.checks)assert.equal(check.passed,true,check.id);
 assert(report.guardChecks>=expected.length*2+100);for(const key of ['librarySha256','catalogSha256'])assert.match(report[key],/^[a-f0-9]{64}$/);
 return {passed:true,checks:expected.length,guardChecks:report.guardChecks,isolated:true,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return process.argv[index+1];};
 const report=JSON.parse(await fs.readFile(option('--report')));const app=option('--app');
 assert.equal(report.catalogSha256,hash(await fs.readFile(path.join(app,'Orchard.Catalog.dll'))));assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));
 const audit={...auditWorkspaceReport(report),catalogSha256:report.catalogSha256,librarySha256:report.librarySha256};await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
}
