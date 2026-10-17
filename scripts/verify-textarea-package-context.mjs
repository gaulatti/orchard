import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const native=option('--native'),output=option('--output'),read=async file=>JSON.parse((await fs.readFile(path.join(native,file),'utf8')).replace(/^\uFEFF/,''));
const build=await read('build.json'),runtime=await read('runtime.json');assert.equal(build.buildPassed,true);assert.equal(runtime.passed,true);assert.equal(runtime.architecture,'X64');assert.equal(runtime.osArchitecture,'X64');
assert.equal(build.independentSource,true);assert.equal(build.projectReference,false);assert.equal(build.orchardStartedUncached,true);assert.equal(build.isolatedSourceRemovedBeforeLaunch,true);assert.equal(build.orchardCacheRemovedBeforeLaunch,true);
assert.equal(path.resolve(runtime.assembly),path.resolve(build.appRoot,'Orchard.dll'));
const hash=async file=>crypto.createHash('sha256').update(await fs.readFile(file)).digest('hex');assert.equal(await hash(runtime.assembly),build.assemblySha256);assert.equal(await hash(path.join(build.appRoot,'resources.pri')),build.mergedApplicationPriSha256);
for(const scheme of['Light','Dark']){
 const report=await read(`consumer-textarea-context-${scheme}.json`);assert.equal(report.passed,true);assert.equal(report.phases.length,3);
 for(const[index,id]of['default','local-null','restored'].entries()){
  const phase=report.phases[index];assert.equal(phase.phase,id);assert.equal(phase.expected,id!=='local-null');assert.equal(phase.isOpen,phase.expected);assert.equal(phase.downs,1);assert.equal(phase.ups,1);assert.equal(phase.exact,true);assert.equal(phase.handlersDetached,true);if(phase.expected)assert.equal(phase.constrained,true);
 }
}
const report={passed:true,version:build.version,librarySource:build.librarySource,packageSha256:build.packageSha256,contextPhases:6,nativeContextKeyPairs:6,independentSource:true,orchardUncachedOnRestore:true,sourceAndOrchardCacheRemovedBeforeLaunch:true,externalSdkDependencyCacheReused:build.externalSdkDependencyCacheReused,nativeARM64Verified:false,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
