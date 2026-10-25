import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {auditWorkspaceReport} from './verify-hidden-workspaces.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const original=JSON.parse(await fs.readFile(option('--report')));auditWorkspaceReport(original);
const rejected=[];
function reject(name,mutate){const report=structuredClone(original);mutate(report);assert.throws(()=>auditWorkspaceReport(report),name);rejected.push(name);}
for(const key of ['passed','hiddenWindow','isolated'])reject(key,r=>{r[key]=false;});
for(const key of ['activateCalled','showCalled','focusCalled','inputSent','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])reject(key,r=>{r[key]=true;});
reject('activation event',r=>{r.activationEvents=1;});reject('isolation failure',r=>{r.isolationFailure='Became visible';});reject('run failure',r=>{r.failure='Interrupted';});
reject('resource diagnostic',r=>{r.resourceErrors=['Missing resource'];});reject('binding diagnostic',r=>{r.bindingErrors=['Invalid binding'];});
reject('foreign architecture',r=>{r.processArchitecture='Arm64';r.osArchitecture='X64';});reject('invalid native architecture',r=>{r.processArchitecture=r.osArchitecture='X86';});
reject('missing scenario',r=>{r.checks.pop();});reject('duplicate scenario',r=>{r.checks[0]=r.checks[1];});reject('failed scenario',r=>{r.checks[0].passed=false;});
reject('insufficient isolation guards',r=>{r.guardChecks=1;});reject('unbound library',r=>{r.librarySha256='';});reject('unbound catalog',r=>{r.catalogSha256='';});
const result={passed:true,negativeCasesRejected:rejected.length,rejected};await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
