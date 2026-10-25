import fs from'node:fs/promises';import assert from'node:assert/strict';import{verifyTableText}from'./verify-table-text.mjs';import{verifyTableTextSource}from'./reference/verify-table-text-source.mjs';
const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const n=JSON.parse(await fs.readFile(arg('--native')+'/table-text.json')),s=JSON.parse(await fs.readFile(arg('--reference')));verifyTableText(n,s);let reports=0;
function reject(change){const copy=structuredClone(n);change(copy);assert.throws(()=>verifyTableText(copy,s));reports++;}
for(const k of ['passed','publicApisOnly'])reject(n=>n[k]=false);
for(const k of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])reject(n=>n[k]=true);
reject(n=>n.focus=1);reject(n=>n.activations=1);reject(n=>n.guards=0);reject(n=>n.cases.pop());reject(n=>n.cases[1]=n.cases[0]);reject(n=>n.histories.pop());reject(n=>n.histories[1]=n.histories[0]);reject(n=>n.pngs.pop());
reject(n=>n.cases[0].snapshot.header[0].raw='changed');reject(n=>n.cases[0].snapshot.header[0].originalAccessibleLabel='changed');reject(n=>n.cases[0].snapshot.header[0].display='WRONG');reject(n=>n.cases[0].snapshot.language='tr-TR');
reject(n=>n.histories.find(h=>h.name==='ordinal-keys').snapshots[0].snapshot.checkboxes[1].checked=true);
reject(n=>n.histories.find(h=>h.name==='ordinal-keys').snapshots[1].snapshot.events=[]);
reject(n=>n.histories.find(h=>h.name==='ordinal-keys').snapshots[0].callerSetUnchanged=false);
reject(n=>n.histories.find(h=>h.name==='dictionary-key-case').snapshots[0].snapshot.body[0].cells[1]='Upper-only');
reject(n=>n.histories.find(h=>h.name==='live-language').snapshots[1].snapshot.header[1].display='IYI STRASSE FFI');
const old=JSON.parse(await fs.readFile('tests/Reference/TableTextCounterexamples.json'));let actual=0;
for(const c of old.cases){const copy=structuredClone(n),target=copy.cases.find(v=>v.scheme===c.scheme&&v.name===c.name);target.snapshot=c.snapshot;assert.throws(()=>verifyTableText(copy,s),assert.AssertionError);actual++;}
for(const h of old.histories){const copy=structuredClone(n),target=copy.histories.find(v=>v.scheme===h.scheme&&v.name===h.name);target.snapshots=h.snapshots;assert.throws(()=>verifyTableText(copy,s),assert.AssertionError);actual++;}
let source=0;for(const change of [s=>s.cases.pop(),s=>s.histories.pop(),s=>s.cases[0].snapshot.header[0].display='WRONG',s=>s.histories.find(h=>h.name==='ordinal-keys').snapshots[0].checkboxes[1].checked=true,s=>s.histories.find(h=>h.name==='dictionary-key-case').snapshots[0].body[0].cells[1]='Upper-only']){const copy=structuredClone(s);change(copy);assert.throws(()=>verifyTableTextSource(copy));source++;}
const result={passed:true,corruptNativeReportsRejected:reports,actualOldRuntimeCounterexamplesRejected:actual,corruptSourceReportsRejected:source};await fs.writeFile(arg('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
