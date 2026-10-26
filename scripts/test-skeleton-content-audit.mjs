import fs from 'node:fs/promises';import assert from 'node:assert/strict';import {verifySkeletonContent} from './verify-skeleton-content.mjs';
const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
const report=JSON.parse(await fs.readFile(option('--report'),'utf8')),reference=JSON.parse(await fs.readFile(option('--reference'),'utf8'));
const identity={librarySha256:report.librarySha256,hostSha256:report.hostSha256};verifySkeletonContent(report,reference,identity);
const contract=(r,id)=>r.contracts.find(c=>c.id===id);
const mutations={
 'original child replaced':r=>{contract(r,'original-content').originalRetained=false;},
 'child action swallowed':r=>{contract(r,'original-content').clicks=0;},
 'child peer hidden':r=>{contract(r,'original-content').childInPeerTree=false;},
 'template restoration loses editor':r=>{contract(r,'template-restoration').editRetained=false;},
 'content binding replaced':r=>{contract(r,'template-restoration').bindingRetained=false;},
 'model cloned':r=>{contract(r,'caller-templates').modelRetained=false;},
 'selector ignored':r=>{contract(r,'caller-templates').selectorCalls=0;},
 'remount duplicates action':r=>{contract(r,'opacity-and-remount').clicks=3;},
 'opacity binding replaced':r=>{contract(r,'opacity-and-remount').opacityBindingRetained=false;},
 'zero columns collapse gaps':r=>{r.states.find(s=>s.id==='table-zero-columns').box.height=0;},
 'capsule becomes ellipse':r=>{r.states.find(s=>s.id==='capsule').shapes[0].radii=[40,40,40,40];},
 'wrong source brush':r=>{r.states[0].shapes[0].brush.color='#FF000000';},
 'wrong brush opacity':r=>{r.states[0].shapes[0].brush.opacity=.21;},
 'missing RTL state':r=>{r.states.splice(r.states.findIndex(s=>s.flow==='RightToLeft'),1);},
 'foreground focus':r=>{r.focus=1;},
 'stale library':r=>{r.librarySha256='0'.repeat(64);},
 'unverified normal pulse accepted':r=>{r.normalPulseVerified=true;},
 'OS settings changed':r=>{r.osSettingsChanged=true;},
};
for(const [name,mutate]of Object.entries(mutations)){const r=structuredClone(report);mutate(r);assert.throws(()=>verifySkeletonContent(r,reference,identity),undefined,name);}
const audit={passed:true,negativeCasesRejected:Object.keys(mutations).length,syntheticResultsAreRuntimeEvidence:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
