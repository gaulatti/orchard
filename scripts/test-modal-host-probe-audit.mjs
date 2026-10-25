import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {auditModalHostProbe} from './verify-modal-host-probe.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
const folder=option('--native'),app=option('--app'),reference=option('--reference'),output=option('--output');
const source=JSON.parse(await fs.readFile(path.join(folder,'modal-host-core.json')));await auditModalHostProbe({folder,app,reference});
const rejected=[];
async function reject(name,change,pngOverride=new Map()){
 const report=structuredClone(source);change(report);let failed=false;try{await auditModalHostProbe({folder,app,reference,report,pngOverride});}catch{failed=true;}
 assert(failed,'Damaged native evidence was accepted: '+name);rejected.push(name);
}
const sample=r=>r.cases.find(c=>c.scheme==='Light'&&c.scenario==='branch-and-layer-order').samples.find(s=>s.stage==='nested-active-child');
for(const field of ['activateCalled','showCalled','focusCalled','inputSent','popupOpened','actualFocusCancellationVerified','actualKeyboardVerified','nativeAlertDialogImplemented','nativeModalAcceptanceVerified','completeSourceRasterCompared'])await reject(field,r=>r[field]=true);
await reject('native-failure',r=>r.failures.push({error:'damaged'}));await reject('missing-case',r=>r.cases.pop());await reject('duplicate-case',r=>r.cases[1]=structuredClone(r.cases[0]));
await reject('failed-case',r=>r.cases[0].passed=false);await reject('guard-evidence',r=>r.guards=0);await reject('different-architecture',r=>r.processArchitecture='Arm64');
for(const field of ['librarySha256','hostSha256','referenceSha256'])await reject(field,r=>r[field]='0'.repeat(64));
await reject('original-binding-replaced',r=>sample(r).bindingsRetained=false);await reject('background-pointer-unblocked',r=>sample(r).backgroundPointerBlocked=false);await reject('background-focus-allowed',r=>sample(r).backgroundFocusAllowed=true);await reject('background-top-hit',r=>sample(r).backgroundHit=true);
await reject('wrong-layer-width',r=>sample(r).layers[0].frame.width=799);await reject('wrong-layer-depth',r=>sample(r).layers[1].zIndex=55);await reject('inactive-layer-input',r=>sample(r).layers[0].pointerAllowed=true);
await reject('wrong-branch-depth',r=>sample(r).layers[1].branches[0].zIndex=59);await reject('wrong-branch-location',r=>sample(r).layers[1].branches[0].frame.x=0);await reject('inactive-branch-input',r=>sample(r).layers[1].branches[0].pointerAllowed=true);
await reject('background-peer-exposed',r=>sample(r).peerNames.push('Original action'));await reject('caller-value-overwritten',r=>r.cases[0].samples.find(s=>s.stage==='deactivated-presence-retained').originalEnabled=true);
await reject('missing-encoded-frame',r=>r.captures.pop());await reject('png-receipt-size',r=>r.captures[0].width=1);await reject('png-path-traversal',r=>r.captures[0].file='../wrong.png');await reject('png-hash',r=>r.captures[0].sha256='0'.repeat(64));
const wrong=await fs.readFile(path.join(folder,'Light-branch-and-layer-order-nested.png')),blocked='Light-blocked.png',wrongHash=crypto.createHash('sha256').update(wrong).digest('hex');
await reject('actual-wrong-native-paint-with-consistent-hash',r=>r.captures.find(c=>c.file===blocked).sha256=wrongHash,new Map([[blocked,wrong]]));
const result={passed:true,negativeCasesRejected:rejected.length,actualWrongNativePaintRejected:true,rejected};await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
