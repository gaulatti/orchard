import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {verifyTooltipBranchModel} from './verify-tooltip-modal-branch.mjs';
const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const folder=arg('--native'),reference=arg('--reference'),output=arg('--output');
const native=JSON.parse(await fs.readFile(folder+'/tooltip-modal-branch.json')),source=JSON.parse(await fs.readFile(reference));verifyTooltipBranchModel(native,source);const rejected=[];
function reject(name,change){const n=structuredClone(native),s=structuredClone(source);change(n,s);assert.throws(()=>verifyTooltipBranchModel(n,s),undefined,name);rejected.push(name);}
reject('missing runtime frame',n=>n.frames.pop());reject('duplicate runtime frame',n=>n.frames[1]=structuredClone(n.frames[0]));
reject('hosted native Popup',n=>n.frames.find(f=>f.mode==='hosted').popupCount=1);reject('wrong source z-index',n=>n.frames.find(f=>f.mode==='hosted').zIndex=50);
reject('fixed body displacement',n=>n.frames[0].body.x++);reject('text width drift',n=>n.frames.find(f=>f.spec.slot==='text').body.width+=1/64);reject('arrow drift',n=>n.frames[0].arrow.y+=.5);
reject('lost description relationship',n=>n.frames[0].describedBy=false);reject('replaced caller content',n=>n.frames[0].originalContentRetained=false);reject('wrong tooltip role',n=>n.frames[0].role='Pane');
reject('acquired focus',n=>n.focus=1);reject('activated window',n=>n.activations=1);reject('hardware input sent',n=>n.hardwareInputSent=true);
reject('inactive tooltip peer exposed',n=>n.cases.find(c=>c.name==='select-suspends-and-resumes-tooltip').inactivePeerExcluded=false);reject('owner unload lost close',n=>n.cases.find(c=>c.name==='tooltip-in-select-and-owner-unload').closedOnce=false);
reject('loaded-window close unverified',n=>n.cases.find(c=>c.name==='window-close-with-live-tooltip').branchReleased=false);reject('field help retained description',n=>n.cases.find(c=>c.name==='field-help-service-lifetime').descriptionReleased=false);
reject('source Escape closed parent',(_,s)=>s.histories[1].after.events.push('close-parent'));reject('source state missing',(_,s)=>s.cases.pop());
const i=process.argv.indexOf('--old-native');let oldReceipt=null;
if(i>=0){const file=process.argv[i+1]+'/tooltip-modal-branch.json',bytes=await fs.readFile(file);assert.throws(()=>verifyTooltipBranchModel(JSON.parse(bytes),source));rejected.push('actual pre-repair native receipt');oldReceipt={path:file,sha256:crypto.createHash('sha256').update(bytes).digest('hex')};}
const result={passed:true,negativeCasesRejected:rejected.length,rejected,oldReceipt,nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({passed:true,negativeCasesRejected:rejected.length,actualOldReceiptRejected:!!oldReceipt}));
