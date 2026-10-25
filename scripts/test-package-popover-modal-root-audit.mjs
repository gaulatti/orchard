import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';import {auditPackagePopup} from './verify-package-popover-modal-root.mjs';
const a=process.argv.slice(2),get=k=>a[a.indexOf(k)+1],folder=get('--native'),pkg=get('--package'),r=JSON.parse(fs.readFileSync(path.join(folder,'package-popover-modal-root.json')));auditPackagePopup(folder,pkg);let rejected=0;
function reject(change){const next=structuredClone(r);change(next);assert.throws(()=>auditPackagePopup(folder,pkg,next));rejected++;}
for(const k of ['activateCalled','showCalled','focusCalled','hardwareInputSent','sourceRasterParityVerified','physicalInputVerified'])reject(x=>x[k]=true);
reject(x=>x.passed=false);reject(x=>x.publicApisOnly=false);reject(x=>x.guards=0);reject(x=>x.focus=1);reject(x=>x.activations=1);reject(x=>x.architecture='Arm64');reject(x=>x.softwareNativeInvokes=0);reject(x=>x.librarySha256='0'.repeat(64));reject(x=>x.hostSha256='0'.repeat(64));reject(x=>x.cases.pop());reject(x=>x.cases.reverse());reject(x=>x.cases[0].passed=false);
for(let i=0;i<3;i++)for(const[k,v]of Object.entries(r.cases[i].evidence))reject(x=>x.cases[i].evidence[k]=typeof v==='boolean'?!v:v===0?1:0);
reject(x=>delete x.cases[1].evidence.modalShielded);reject(x=>x.captures=[]);reject(x=>x.captures[0].sha256='0'.repeat(64));reject(x=>x.captures[0].width=400);
const result={passed:true,negativeCasesRejected:rejected,physicalInputVerified:false};fs.writeFileSync(get('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
