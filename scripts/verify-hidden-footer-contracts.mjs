import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1],name);return process.argv[index+1];};
const native=path.resolve(option('--native')),app=path.resolve(option('--app')),host=option('--host'),architecture=option('--architecture');
assert(['Orchard.Catalog','PackageConsumer'].includes(host));assert(['X64','Arm64'].includes(architecture));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex'),read=async name=>JSON.parse(await fs.readFile(path.join(native,name)));
const report=await read('hidden-footer-contracts.json'),exit=await read('process-exit.json');
assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(report.passed,true);assert.equal(report.diagnosticOnly,true);assert.equal(report.scope,'hidden-footer-public-contracts');
for(const name of ['activateCalled','showCalled','focusCalled','inputSent','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[name],false,name);
assert.equal(report.hiddenWindow,true);assert.equal(report.isolated,true);assert.equal(report.activationEvents,0);assert.equal(report.focusEvents,0);assert(report.guardChecks>=80);
assert.equal(report.processArchitecture,architecture);assert.equal(report.osArchitecture,architecture);assert.deepEqual(report.failures,[]);
const ids=['bottom-right-bound-nullish-scalars','bottom-left-model-template-binding','bottom-right-selector-original-model','original-editor-survives-section-mutations','original-editors-survive-template-replacement','fractional-host-original-slots','custom-link-original-item-command-and-style','keyed-item-replacement-and-retirement','source-raw-first-match-and-explicit-suppression','bound-brand-metadata-and-explicit-null-logo','custom-template-retired-and-remounted-actions','custom-renderer-owned-container-metrics'];
assert.equal(report.checks.length,12);assert.deepEqual(report.checks.map(row=>row.id).sort(),ids.sort());assert(report.checks.every(row=>row.passed===true));

const library=hash(await fs.readFile(path.join(app,'Orchard.dll'))),application=hash(await fs.readFile(path.join(app,host+'.dll')));
assert.equal(report.librarySha256,library);assert.equal(report.hostSha256,application);
assert.equal(hash(await fs.readFile(path.join(native,'app-identity','Orchard.dll'))),library);assert.equal(hash(await fs.readFile(path.join(native,'app-identity',host+'.dll'))),application);
let packageSha256;
if(host==='PackageConsumer'){
 packageSha256=hash(await fs.readFile(path.resolve(option('--package'))));
 const build=await read('result.json'),cleanup=await read('cleanup.json');
 if(Object.hasOwn(build,'buildPassed')){assert.equal(build.buildPassed,true);assert.equal(build.runtimeVerified,false);}else{assert.equal(build.passed,true);assert.equal(build.runtime.passed,true);}
 assert.equal(build.platform.toLowerCase(),architecture.toLowerCase());assert.equal(build.packageSha256,packageSha256);assert.equal(cleanup.removed,true);
 const currentReceipt=Object.hasOwn(build,'isolatedProject');
 const isolated=currentReceipt?build.isolatedProject:build.isolatedConsumer;
 const cleaned=currentReceipt?cleanup.path:cleanup.isolatedConsumer;
 assert.equal(typeof isolated,'string');assert(isolated.length>0);assert.equal(cleaned,isolated);
 await assert.rejects(fs.access(isolated),{code:'ENOENT'});
}
const audit={passed:true,scope:'hidden-footer-public-contracts',checks:12,guardChecks:report.guardChecks,isolated:true,activationEvents:0,focusEvents:0,librarySha256:library,hostSha256:application,packageSha256,processArchitecture:architecture,visibleRuntimeVerified:false,nativeComponentAccepted:false};
await fs.writeFile(path.resolve(option('--output')),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
